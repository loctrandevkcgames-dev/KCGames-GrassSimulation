using System.Collections.Generic;
using System.IO;
using System.Text;
using GrassSimulation.Gameplay;
using UnityEditor;
using UnityEngine;

namespace GrassSimulation.Editor
{
    public static class LevelPipeline
    {
        public const string SPEC_PATH = "Assets/GrassSimulation/Data/Levels/levels-d1.csv";
        public const string LAYOUT_DIRECTORY = "Assets/GrassSimulation/Data/Levels/Layouts";
        public const string DATABASE_DIRECTORY = "Assets/GrassSimulation/Addressables/assets-shared/database";
        public const string LEVEL_DIRECTORY = DATABASE_DIRECTORY + "/Levels";
        public const string CATALOG_PATH = DATABASE_DIRECTORY + "/LevelCatalog.asset";
        public const string PLANT_CATALOG_PATH = DATABASE_DIRECTORY + "/PlantCatalog.asset";

        public static string LayoutPath(LevelSpec spec)
            => $"{LAYOUT_DIRECTORY}/{spec.Id}.txt";

        public static string LevelPath(LevelSpec spec)
            => $"{LEVEL_DIRECTORY}/Level{spec.Order:00}.asset";

        public static LevelPipelineReport BakeAll()
            => Bake(null);

        public static LevelPipelineReport Bake(ICollection<int> orders)
        {
            var text = new StringBuilder();
            var failed = 0;
            var passed = 0;

            if (TryLoad(text, out var specs, out var plants) == false)
            {
                return new LevelPipelineReport(0, 1, text.ToString());
            }

            for (var i = 0; i < specs.Count; i++)
            {
                var spec = specs[i];

                if (orders != null && orders.Contains(spec.Order) == false)
                {
                    continue;
                }

                if (File.Exists(LayoutPath(spec)) == false)
                {
                    continue;
                }

                var isOk = BakeOne(text, spec, plants);

                passed += isOk ? 1 : 0;
                failed += isOk ? 0 : 1;
            }

            RebuildCatalog(specs);
            AssetDatabase.SaveAssets();
            return new LevelPipelineReport(passed, failed, text.ToString());
        }

        public static LevelPipelineReport ValidateAll()
        {
            var text = new StringBuilder();
            var failed = 0;
            var passed = 0;

            if (TryLoad(text, out var specs, out var plants) == false)
            {
                return new LevelPipelineReport(0, 1, text.ToString());
            }

            for (var i = 0; i < specs.Count; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath(specs[i]));

                if (level == null || File.Exists(LayoutPath(specs[i])) == false)
                {
                    continue;
                }

                var isOk = Validate(text, specs[i], level, plants);

                passed += isOk ? 1 : 0;
                failed += isOk ? 0 : 1;
            }

            return new LevelPipelineReport(passed, failed, text.ToString());
        }

        public static bool TryLoad(StringBuilder text, out List<LevelSpec> specs, out PlantDefinition[] plants)
        {
            specs = null;
            plants = null;

            var catalog = AssetDatabase.LoadAssetAtPath<PlantCatalog>(PLANT_CATALOG_PATH);

            if (catalog == null)
            {
                text.AppendLine($"Missing plant catalog at {PLANT_CATALOG_PATH}.");
                return false;
            }

            var parsed = LevelSpecTable.Parse(File.ReadAllText(SPEC_PATH));

            if (parsed.TryGetError(out var error))
            {
                text.AppendLine(error.ToMessage());
                return false;
            }

            parsed.TryGetValue(out specs);
            plants = catalog.Plants.ToArray();
            return true;
        }

        private static bool BakeOne(StringBuilder text, LevelSpec spec, PlantDefinition[] plants)
        {
            var layoutText = File.ReadAllText(LayoutPath(spec));
            var layoutResult = LayoutText.Parse(spec.Id, layoutText, spec.Width, spec.Length);

            if (layoutResult.TryGetError(out var layoutError))
            {
                text.AppendLine($"{spec.Id} ERROR {layoutError.ToMessage()}");
                return false;
            }

            layoutResult.TryGetValue(out var layout);

            var bakeResult = LevelBaker.Bake(spec, layout, plants);

            if (bakeResult.TryGetError(out var bakeError))
            {
                text.AppendLine($"{spec.Id} ERROR {bakeError.ToMessage()}");
                return false;
            }

            bakeResult.TryGetValue(out var baked);

            var level = LoadOrCreate(spec);

            LevelAssetWriter.Apply(level, spec, baked);
            return Validate(text, spec, level, plants);
        }

        private static bool Validate(
              StringBuilder text
            , LevelSpec spec
            , LevelDefinition level
            , PlantDefinition[] plants
        )
        {
            var layout = LevelLayout.From(level);
            var issues = LevelValidator.Validate(layout, spec, plants, in LevelValidationRules.Default);

            if (issues.Count == 0)
            {
                text.AppendLine($"{spec.Id} OK");
                return true;
            }

            text.AppendLine($"{spec.Id} FAIL");

            for (var i = 0; i < issues.Count; i++)
            {
                text.AppendLine($"  {issues[i]}");
            }

            return false;
        }

        private static LevelDefinition LoadOrCreate(LevelSpec spec)
        {
            var path = LevelPath(spec);
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);

            if (level != null)
            {
                return level;
            }

            level = ScriptableObject.CreateInstance<LevelDefinition>();
            AssetDatabase.CreateAsset(level, path);
            return level;
        }

        private static void RebuildCatalog(List<LevelSpec> specs)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(CATALOG_PATH);
            var levels = new List<LevelDefinition>();

            for (var i = 0; i < specs.Count; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelPath(specs[i]));

                if (level != null)
                {
                    levels.Add(level);
                }
            }

            LevelAssetWriter.WriteCatalog(catalog, levels.ToArray());
        }
    }
}
