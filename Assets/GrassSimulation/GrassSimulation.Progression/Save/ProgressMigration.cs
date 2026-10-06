using System;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    internal static class ProgressMigration
    {
        internal static bool TryUpgrade(ProgressSave save)
        {
            if (save.Version == 1)
            {
                MigrateFromV1(save);
            }

            return save.Version == ProgressSave.CURRENT_VERSION;
        }

        internal static StarFlags FromStarCount(int count)
        {
            var clamped = Math.Clamp(count, 0, StarRules.MAX_STARS);

            return (StarFlags)((1 << clamped) - 1);
        }

        private static void MigrateFromV1(ProgressSave save)
        {
            var levels = save.Levels;

            if (save.BestStars != null)
            {
                foreach (var pair in save.BestStars)
                {
                    levels[pair.Key] = new LevelRecord { Stars = FromStarCount(pair.Value) };
                }
            }

            if (save.CompletedLevels != null)
            {
                var count = save.CompletedLevels.Count;

                for (var i = 0; i < count; i++)
                {
                    var id = save.CompletedLevels[i];

                    if (levels.TryGetValue(id, out var record) == false)
                    {
                        record = new LevelRecord();
                        levels[id] = record;
                    }

                    record.Completed = true;
                    record.Stars |= StarFlags.Goal;
                }
            }

            save.CompletedLevels = null;
            save.BestStars = null;
            save.Version = ProgressSave.CURRENT_VERSION;
        }
    }
}
