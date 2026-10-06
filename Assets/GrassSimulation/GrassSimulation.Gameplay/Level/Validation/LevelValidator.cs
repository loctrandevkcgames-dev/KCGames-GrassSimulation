using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public static class LevelValidator
    {
        private const int SAMPLE_LIMIT = 3;

        public static List<LevelIssue> Validate(
              LevelLayout layout
            , LevelSpec spec
            , ReadOnlySpan<PlantDefinition> plants
            , in LevelValidationRules rules
        )
        {
            var issues = new List<LevelIssue>();
            var byKind = IndexPlants(plants);
            var counts = CountPlaced(layout);

            CheckCounts(issues, layout, spec, counts);
            CheckQuotas(issues, layout, spec, byKind, counts, in rules);
            CheckXp(issues, layout, spec, byKind, counts, in rules);
            CheckTimer(issues, layout, spec, in rules);

            var targets = CollectTargets(layout, byKind);
            var standard = new LevelWalkMap(layout, rules.BodyRadius, rules.GridStep, 0f);

            CheckSpawn(issues, layout, byKind, standard);

            if (standard.IsSpawnFree)
            {
                ReportUnreachable(issues, LevelRule.Reach, targets, standard, in rules, null);
                CheckPath(issues, layout, spec, targets, in rules);
            }

            return issues;
        }

        private static PlantDefinition[] IndexPlants(ReadOnlySpan<PlantDefinition> plants)
        {
            var byKind = new PlantDefinition[PlantKindExtensions.Length];

            for (var i = 0; i < plants.Length; i++)
            {
                byKind[(int)plants[i].Kind] = plants[i];
            }

            return byKind;
        }

        private static int[] CountPlaced(LevelLayout layout)
        {
            var counts = new int[PlantKindExtensions.Length];

            for (var i = 0; i < layout.Kinds.Length; i++)
            {
                counts[(int)layout.Kinds[i]]++;
            }

            for (var i = 0; i < layout.Plants.Length; i++)
            {
                counts[(int)layout.Plants[i].Kind]++;
            }

            return counts;
        }

        private static bool IsLegacy(PlantKind kind)
            => kind == PlantKind.LowBush || kind == PlantKind.HardBush;

        private static void CheckCounts(List<LevelIssue> issues, LevelLayout layout, LevelSpec spec, int[] counts)
        {
            for (var index = (int)PlantKind.Grass; index < counts.Length; index++)
            {
                var kind = (PlantKind)index;

                if (kind == PlantKind.ProtectedFlower)
                {
                    continue;
                }

                var expected = IsLegacy(kind) ? 0 : spec.GetCount(kind);

                if (counts[index] != expected)
                {
                    issues.Add(new LevelIssue(LevelRule.Counts, $"{kind}: placed {counts[index]}, spec {expected}."));
                }
            }

            CheckBeds(issues, layout, spec, counts);
        }

        private static void CheckBeds(List<LevelIssue> issues, LevelLayout layout, LevelSpec spec, int[] counts)
        {
            if (layout.Beds.Length != spec.BedCount)
            {
                issues.Add(new LevelIssue(LevelRule.Counts, $"Beds: {layout.Beds.Length}, spec {spec.BedCount}."));
            }

            var area = 0;

            for (var i = 0; i < layout.Beds.Length; i++)
            {
                area += layout.Beds[i].width * layout.Beds[i].height;
            }

            var protectedCells = counts[(int)PlantKind.ProtectedFlower];

            if (area != protectedCells)
            {
                issues.Add(new LevelIssue(
                      LevelRule.Counts
                    , $"Protected cells {protectedCells} differ from the bed area {area}."
                ));
            }
        }

        private static void CheckQuotas(
              List<LevelIssue> issues
            , LevelLayout layout
            , LevelSpec spec
            , PlantDefinition[] byKind
            , int[] counts
            , in LevelValidationRules rules
        )
        {
            if (spec.HasSameQuotas(layout.Quotas) == false)
            {
                issues.Add(new LevelIssue(LevelRule.Quota, "The level quotas differ from the spec."));
            }

            for (var i = 0; i < layout.Quotas.Length; i++)
            {
                var quota = layout.Quotas[i];
                var placed = counts[(int)quota.Kind];

                if (quota.Amount > placed / rules.QuotaSurplus + 1e-4f)
                {
                    issues.Add(new LevelIssue(
                          LevelRule.Quota
                        , $"{quota.Kind} quota {quota.Amount} needs more than {placed} plants (x{rules.QuotaSurplus})."
                    ));
                }

                if (byKind[(int)quota.Kind].RequiredTier > layout.MaxTier)
                {
                    issues.Add(new LevelIssue(LevelRule.Quota, $"{quota.Kind} needs a tier above the level maximum."));
                }
            }
        }

        private static void CheckXp(
              List<LevelIssue> issues
            , LevelLayout layout
            , LevelSpec spec
            , PlantDefinition[] byKind
            , int[] counts
            , in LevelValidationRules rules
        )
        {
            var thresholds = rules.XpThresholds;
            var xpByTier = new int[thresholds.Length + 2];

            if (layout.MaxTier != spec.MaxTier)
            {
                issues.Add(new LevelIssue(LevelRule.Xp, $"Max tier {layout.MaxTier}, spec {spec.MaxTier}."));
            }

            for (var index = (int)PlantKind.Grass; index < counts.Length; index++)
            {
                var plant = byKind[index];

                if (plant.IsProtected || counts[index] == 0 || plant.RequiredTier >= xpByTier.Length)
                {
                    continue;
                }

                if (plant.RequiredTier > thresholds.Length && plant.Xp != 0)
                {
                    issues.Add(new LevelIssue(LevelRule.Xp, $"{plant.Kind} is the top tier but gives XP."));
                }

                xpByTier[plant.RequiredTier] += counts[index] * plant.Xp;
            }

            var cumulative = 0;

            for (var tier = 1; tier < layout.MaxTier && tier <= thresholds.Length; tier++)
            {
                cumulative += xpByTier[tier];

                var needed = thresholds[tier - 1] * rules.XpSurplus;

                if (cumulative < needed)
                {
                    issues.Add(new LevelIssue(
                          LevelRule.Xp
                        , $"XP from tier <= {tier} is {cumulative}, below {needed} needed for tier {tier + 1}."
                    ));
                }
            }

            CheckXpCeiling(issues, xpByTier, layout.MaxTier, thresholds);
        }

        private static void CheckXpCeiling(List<LevelIssue> issues, int[] xpByTier, int maxTier, int[] thresholds)
        {
            if (maxTier < 1 || maxTier > thresholds.Length)
            {
                return;
            }

            var total = 0;

            for (var tier = 1; tier < xpByTier.Length; tier++)
            {
                total += xpByTier[tier];
            }

            if (total >= thresholds[maxTier - 1])
            {
                issues.Add(new LevelIssue(
                      LevelRule.Xp
                    , $"Total XP {total} reaches tier {maxTier + 1} ({thresholds[maxTier - 1]}) above the level maximum."
                ));
            }
        }

        private static void CheckTimer(
              List<LevelIssue> issues
            , LevelLayout layout
            , LevelSpec spec
            , in LevelValidationRules rules
        )
        {
            var isUntimedType = layout.Type == LevelType.Tutorial || layout.Type == LevelType.Relax;

            if (layout.Type != spec.Type)
            {
                issues.Add(new LevelIssue(LevelRule.Timer, $"Type {layout.Type}, spec {spec.Type}."));
            }

            if (isUntimedType)
            {
                if (layout.TimeLimit != 0f || spec.IsTimed)
                {
                    issues.Add(new LevelIssue(LevelRule.Timer, $"{layout.Type} levels must be untimed."));
                }

                return;
            }

            var isOnSpec = Mathf.Approximately(layout.TimeLimit, spec.SuggestedTimer);

            if (layout.TimeLimit < rules.MinTimer || isOnSpec == false)
            {
                issues.Add(new LevelIssue(
                      LevelRule.Timer
                    , $"Timer {layout.TimeLimit}s must be >= {rules.MinTimer}s and equal {spec.SuggestedTimer}s."
                ));
            }
        }

        private static void CheckSpawn(
              List<LevelIssue> issues
            , LevelLayout layout
            , PlantDefinition[] byKind
            , LevelWalkMap standard
        )
        {
            if (standard.IsSpawnFree == false)
            {
                issues.Add(new LevelIssue(LevelRule.Spawn, "The spawn is outside the map or inside an obstacle."));
                return;
            }

            var cellX = Mathf.FloorToInt(layout.Spawn.x / layout.CellSize);
            var cellZ = Mathf.FloorToInt(layout.Spawn.y / layout.CellSize);
            var kind = layout.Kinds[cellZ * layout.CellsX + cellX];

            if (kind != PlantKind.None)
            {
                issues.Add(new LevelIssue(LevelRule.Spawn, $"The spawn stands on {kind}."));
            }

            for (var i = 0; i < layout.Beds.Length; i++)
            {
                if (layout.Beds[i].Contains(new Vector2Int(cellX, cellZ)))
                {
                    issues.Add(new LevelIssue(LevelRule.Spawn, $"The spawn is inside bed {i}."));
                }
            }

            for (var i = 0; i < layout.Plants.Length; i++)
            {
                var plant = layout.Plants[i];
                var distance = Vector2.Distance(plant.Position, layout.Spawn);

                if (distance < byKind[(int)plant.Kind].CutZoneRadius)
                {
                    issues.Add(new LevelIssue(LevelRule.Spawn, $"The spawn is inside {plant.Kind} {i}."));
                }
            }
        }

        private static List<Target> CollectTargets(LevelLayout layout, PlantDefinition[] byKind)
        {
            var targets = new List<Target>();

            for (var i = 0; i < layout.Kinds.Length; i++)
            {
                var kind = layout.Kinds[i];

                if (kind == PlantKind.None || byKind[(int)kind].IsProtected)
                {
                    continue;
                }

                var center = new Vector2(
                      (i % layout.CellsX + 0.5f) * layout.CellSize
                    , (i / layout.CellsX + 0.5f) * layout.CellSize
                );

                targets.Add(new Target(kind, center, byKind[(int)kind].CutZoneRadius));
            }

            for (var i = 0; i < layout.Plants.Length; i++)
            {
                var plant = layout.Plants[i];

                targets.Add(new Target(plant.Kind, plant.Position, byKind[(int)plant.Kind].CutZoneRadius));
            }

            return targets;
        }

        private static void CheckPath(
              List<LevelIssue> issues
            , LevelLayout layout
            , LevelSpec spec
            , List<Target> targets
            , in LevelValidationRules rules
        )
        {
            var quotaKinds = new HashSet<PlantKind>();

            for (var i = 0; i < layout.Quotas.Length; i++)
            {
                quotaKinds.Add(layout.Quotas[i].Kind);
            }

            var path = new LevelWalkMap(layout, rules.PathRadius, rules.GridStep, 0f);

            ReportUnreachable(issues, LevelRule.PathWidth, targets, path, in rules, quotaKinds);

            if (layout.Beds.Length == 0)
            {
                return;
            }

            var inflate = rules.MaxBladeReach(layout.MaxTier, spec.Order) + rules.BedMargin;
            var clear = new LevelWalkMap(layout, rules.PathRadius, rules.GridStep, inflate);

            if (clear.IsSpawnFree == false)
            {
                issues.Add(new LevelIssue(LevelRule.ProtectedClearance, "The spawn is too close to a bed."));
                return;
            }

            ReportUnreachable(issues, LevelRule.ProtectedClearance, targets, clear, in rules, quotaKinds);
        }

        private static void ReportUnreachable(
              List<LevelIssue> issues
            , LevelRule rule
            , List<Target> targets
            , LevelWalkMap map
            , in LevelValidationRules rules
            , HashSet<PlantKind> onlyKinds
        )
        {
            var missed = new Dictionary<PlantKind, List<Vector2>>();

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                if (onlyKinds != null && onlyKinds.Contains(target.Kind) == false)
                {
                    continue;
                }

                if (map.CanCut(target.Position, rules.BaseReach + target.ZoneRadius))
                {
                    continue;
                }

                if (missed.TryGetValue(target.Kind, out var list) == false)
                {
                    list = new List<Vector2>();
                    missed[target.Kind] = list;
                }

                list.Add(target.Position);
            }

            foreach (var pair in missed)
            {
                issues.Add(new LevelIssue(
                      rule
                    , $"{pair.Value.Count} {pair.Key} cannot be cut, e.g. {Sample(pair.Value)}."
                ));
            }
        }

        private static string Sample(List<Vector2> positions)
        {
            var count = Mathf.Min(positions.Count, SAMPLE_LIMIT);
            var parts = new string[count];

            for (var i = 0; i < count; i++)
            {
                parts[i] = $"({positions[i].x:0.0}, {positions[i].y:0.0})";
            }

            return string.Join(" ", parts);
        }

        private readonly record struct Target(PlantKind Kind, Vector2 Position, float ZoneRadius);
    }
}
