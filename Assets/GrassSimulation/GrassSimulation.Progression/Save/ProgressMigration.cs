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

            if (save.Version == 2)
            {
                MigrateFromV2(save);
            }

            if (save.Version == 3)
            {
                MigrateFromV3(save);
            }

            save.EnsureMachines();
            save.EnsureBoosters();
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
            save.Version = 2;
        }

        private static void MigrateFromV2(ProgressSave save)
        {
            save.EnsureMachines();
            save.Version = 3;
        }

        // Version 3 recorded booster unlocks as granted without any stock. Grant the D1 default gifts once.
        private static void MigrateFromV3(ProgressSave save)
        {
            save.EnsureBoosters();

            var gifts = GameRulesValues.Default;

            GrantMissingGift(save, UnlockKind.TurboBooster, BoosterKind.Turbo, gifts.BoosterGiftTurbo);
            GrantMissingGift(save, UnlockKind.ExtraTimeBooster, BoosterKind.ExtraTime, gifts.BoosterGiftExtraTime);
            save.Version = ProgressSave.CURRENT_VERSION;
        }

        private static void GrantMissingGift(ProgressSave save, UnlockKind unlock, BoosterKind booster, int gift)
        {
            var suffix = $":{unlock}";
            var grantCount = 0;

            foreach (var id in save.GrantedUnlocks)
            {
                if (id.EndsWith(suffix, StringComparison.Ordinal))
                {
                    grantCount++;
                }
            }

            if (grantCount > 0)
            {
                save.BoosterStock[booster.ToStringFast()] = gift * grantCount;
            }
        }
    }
}
