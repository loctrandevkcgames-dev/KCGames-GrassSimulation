using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EncosyTower.Common;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Editor
{
    public static class LevelSpecTable
    {
        private const string FIRST_PLANT_COLUMN = "grass";
        private const string TIMED_VALUE = "Có";

        private static readonly (string Column, PlantKind Kind)[] s_plantColumns = {
            ("grass", PlantKind.Grass),
            ("flower", PlantKind.HarvestFlower),
            ("thick", PlantKind.ThickGrass),
            ("bush_low", PlantKind.BushLow),
            ("veg", PlantKind.Vegetable),
            ("bush_big", PlantKind.BushBig),
            ("melon", PlantKind.Melon),
            ("fruit_tree", PlantKind.FruitTree),
            ("fruit_giant", PlantKind.GiantFruit),
        };

        private static readonly (string Name, PlantKind Kind)[] s_quotaNames = {
            ("Cỏ", PlantKind.Grass),
            ("Hoa", PlantKind.HarvestFlower),
            ("Cỏ dày", PlantKind.ThickGrass),
            ("Bụi thấp", PlantKind.BushLow),
            ("Rau", PlantKind.Vegetable),
            ("Bụi lớn", PlantKind.BushBig),
            ("Dưa/bí", PlantKind.Melon),
            ("Cây quả", PlantKind.FruitTree),
            ("Quả KL", PlantKind.GiantFruit),
        };

        private static readonly (string Name, LevelType Type)[] s_typeNames = {
            ("Hướng dẫn", LevelType.Tutorial),
            ("Thường", LevelType.Normal),
            ("Khó", LevelType.Hard),
            ("Thư giãn", LevelType.Relax),
        };

        private static readonly Regex s_amount = new(@"\d+");

        public static Result<List<LevelSpec>, LevelBuildError> Parse(string csv)
        {
            var rows = CsvRows.Parse(csv);

            if (rows.Count < 2)
            {
                return Fail(string.Empty, "the table has no rows.");
            }

            var columns = new Dictionary<string, int>();

            for (var i = 0; i < rows[0].Length; i++)
            {
                columns[rows[0][i].Trim()] = i;
            }

            var specs = new List<LevelSpec>();

            for (var i = 1; i < rows.Count; i++)
            {
                var result = ParseRow(new Row(rows[i], columns));

                if (result.TryGetValue(out var spec))
                {
                    specs.Add(spec);
                    continue;
                }

                result.TryGetError(out var error);
                return Result<List<LevelSpec>, LevelBuildError>.Err(error);
            }

            return Result<List<LevelSpec>, LevelBuildError>.Succeed(specs);
        }

        private static Result<LevelSpec, LevelBuildError> ParseRow(Row row)
        {
            var id = row.Text("id");

            if (IsMissingColumns(row))
            {
                return FailRow(id, "a required column is missing.");
            }

            if (TryFindType(row.Text("type"), out var type) == false)
            {
                return FailRow(id, $"unknown level type '{row.Text("type")}'.");
            }

            var counts = new int[PlantKindExtensions.Length];

            for (var i = 0; i < s_plantColumns.Length; i++)
            {
                counts[(int)s_plantColumns[i].Kind] = row.Number(s_plantColumns[i].Column);
            }

            var quotas = new List<QuotaSettings>();

            if (TryReadQuotas(row, quotas) == false)
            {
                return FailRow(id, "a quota names an unknown plant.");
            }

            var isTimed = row.Text("timer") == TIMED_VALUE;

            var spec = new LevelSpec(
                  Id: id
                , Order: row.Number("order")
                , Step: row.Number("step")
                , Name: row.Text("name")
                , Decision: row.Text("decision")
                , Type: type
                , Width: row.Number("width")
                , Length: row.Number("length")
                , ZoneCount: row.Number("zones")
                , BedCount: row.Number("beds")
                , MaxTier: row.Number("max_tier")
                , PlantCounts: counts
                , Quotas: quotas.ToArray()
                , IsTimed: isTimed
                , SuggestedTimer: isTimed ? row.Number("timer_suggested") : 0f
                , Unlock: ParseUnlock(row.Text("unlock"))
            );

            return Result<LevelSpec, LevelBuildError>.Succeed(spec);
        }

        private static bool IsMissingColumns(Row row)
            => row.Has(FIRST_PLANT_COLUMN) == false || row.Has("q1_kind") == false || row.Has("timer") == false;

        private static bool TryReadQuotas(Row row, List<QuotaSettings> quotas)
        {
            for (var i = 1; i <= 3; i++)
            {
                if (TryReadQuota(row, $"q{i}_kind", $"q{i}_amount", isBonus: false, quotas) == false)
                {
                    return false;
                }
            }

            return TryReadQuota(row, "side_kind", "side_amount", isBonus: true, quotas);
        }

        private static bool TryReadQuota(
              Row row
            , string kindColumn
            , string amountColumn
            , bool isBonus
            , List<QuotaSettings> quotas
        )
        {
            var name = row.Text(kindColumn);

            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            if (TryFindKind(name, out var kind) == false)
            {
                return false;
            }

            quotas.Add(new QuotaSettings { Kind = kind, Amount = row.Number(amountColumn), IsBonus = isBonus });
            return true;
        }

        private static bool TryFindKind(string name, out PlantKind kind)
        {
            var normalized = name.Normalize(NormalizationForm.FormC);

            for (var i = 0; i < s_quotaNames.Length; i++)
            {
                if (s_quotaNames[i].Name.Normalize(NormalizationForm.FormC) == normalized)
                {
                    kind = s_quotaNames[i].Kind;
                    return true;
                }
            }

            kind = PlantKind.None;
            return false;
        }

        private static bool TryFindType(string name, out LevelType type)
        {
            var normalized = name.Normalize(NormalizationForm.FormC);

            for (var i = 0; i < s_typeNames.Length; i++)
            {
                if (s_typeNames[i].Name.Normalize(NormalizationForm.FormC) == normalized)
                {
                    type = s_typeNames[i].Type;
                    return true;
                }
            }

            type = LevelType.Normal;
            return false;
        }

        private static UnlockSettings ParseUnlock(string text)
        {
            var amountMatch = s_amount.Match(text);
            var amount = amountMatch.Success ? int.Parse(amountMatch.Value, CultureInfo.InvariantCulture) : 0;

            if (text.Contains("Extra Time"))
            {
                return new UnlockSettings { Kind = UnlockKind.ExtraTimeBooster, Amount = amount };
            }

            if (text.Contains("Turbo"))
            {
                return new UnlockSettings { Kind = UnlockKind.TurboBooster, Amount = amount };
            }

            if (text.Contains("Wide"))
            {
                return new UnlockSettings { Kind = UnlockKind.WideMachine, Amount = 0 };
            }

            return default;
        }

        private static Result<List<LevelSpec>, LevelBuildError> Fail(string id, string reason)
            => Result<List<LevelSpec>, LevelBuildError>.Err(new LevelBuildError.SpecInvalid(id, reason));

        private static Result<LevelSpec, LevelBuildError> FailRow(string id, string reason)
            => Result<LevelSpec, LevelBuildError>.Err(new LevelBuildError.SpecInvalid(id, reason));

        private readonly record struct Row(string[] Cells, Dictionary<string, int> Columns)
        {
            public bool Has(string column)
                => Columns.ContainsKey(column);

            public string Text(string column)
                => Columns.TryGetValue(column, out var index) && index < Cells.Length
                    ? Cells[index].Trim()
                    : string.Empty;

            public int Number(string column)
            {
                var text = Text(column);
                var isNumber = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value);

                return isNumber ? value : 0;
            }
        }
    }
}
