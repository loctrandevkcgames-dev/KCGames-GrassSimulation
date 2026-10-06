using System;
using EncosyTower.Common;

namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelSnapshot(
          LevelId Level
        , int LevelIndex
        , int LevelCount
        , LevelState State
        , bool IsPaused
        , float RemainingTime
        , float TimeLimit
        , float ClearedFraction
        , int Tier
        , int Xp
        , int TierFloorXp
        , Option<int> NextThresholdXp
        , int PendingUpgrades
        , int ProtectedHits
        , int ProtectedHitLimit
        , bool CountsProtectedHits
        , int QuotaCount
        , QuotaSnapshot Quota0
        , QuotaSnapshot Quota1
        , QuotaSnapshot Quota2
        , QuotaSnapshot Quota3
        , MachineStats Stats
        , int UpgradeOptionCount
        , string Upgrade0Id
        , MachineStats Upgrade0Stats
        , string Upgrade1Id
        , MachineStats Upgrade1Stats
    )
    {
        public const int MAX_QUOTAS = 4;
        public const int MAX_UPGRADE_OPTIONS = 2;

        public static LevelSnapshot From(LevelSession session, LevelDefinition level, int levelIndex, int levelCount)
        {
            var growth = session.Growth;
            var objectives = session.Objectives;
            var quotaCount = Math.Min(objectives.QuotaCount, MAX_QUOTAS);
            var optionCount = Math.Min(growth.UpgradeOptionCount, MAX_UPGRADE_OPTIONS);
            Option<int> nextThreshold = growth.TryGetNextThreshold(out var threshold) ? threshold : Option.None;

            return new LevelSnapshot(
                  level.Id
                , levelIndex
                , levelCount
                , session.State
                , session.IsPaused
                , session.RemainingTime
                , level.TimeLimit
                , session.ClearedFraction
                , growth.Tier
                , growth.Xp
                , growth.TierFloorXp
                , nextThreshold
                , growth.PendingUpgrades
                , session.Protection.Hits
                , session.ProtectedHitLimit
                , session.CountsProtectedHits
                , quotaCount
                , ReadQuota(objectives, index: 0, count: quotaCount)
                , ReadQuota(objectives, index: 1, count: quotaCount)
                , ReadQuota(objectives, index: 2, count: quotaCount)
                , ReadQuota(objectives, index: 3, count: quotaCount)
                , growth.Stats
                , optionCount
                , ReadUpgradeId(growth, option: 0, count: optionCount)
                , ReadUpgradeStats(growth, option: 0, count: optionCount)
                , ReadUpgradeId(growth, option: 1, count: optionCount)
                , ReadUpgradeStats(growth, option: 1, count: optionCount)
            );
        }

        public QuotaSnapshot GetQuota(int index)
        {
            return index switch {
                0 => Quota0,
                1 => Quota1,
                2 => Quota2,
                3 => Quota3,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        public string GetUpgradeId(int option)
        {
            return option switch {
                0 => Upgrade0Id,
                1 => Upgrade1Id,
                _ => throw new ArgumentOutOfRangeException(nameof(option)),
            };
        }

        public MachineStats GetUpgradeStats(int option)
        {
            return option switch {
                0 => Upgrade0Stats,
                1 => Upgrade1Stats,
                _ => throw new ArgumentOutOfRangeException(nameof(option)),
            };
        }

        private static QuotaSnapshot ReadQuota(LevelObjectives objectives, int index, int count)
        {
            if (index >= count)
            {
                return default;
            }

            ref readonly var quota = ref objectives.GetQuota(index);

            return new QuotaSnapshot(
                  quota.Kind
                , quota.Amount
                , objectives.GetQuotaProgress(index)
                , quota.IsBonus
                , objectives.IsQuotaMet(index)
            );
        }

        private static string ReadUpgradeId(MowerGrowth growth, int option, int count)
            => option < count ? growth.GetUpgrade(option).Id : default;

        private static MachineStats ReadUpgradeStats(MowerGrowth growth, int option, int count)
            => option < count ? growth.GetStatsWithUpgrade(option) : growth.Stats;
    }
}
