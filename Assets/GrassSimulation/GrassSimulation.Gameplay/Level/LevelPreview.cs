using System;

namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelPreview(
          LevelId Level
        , int LevelIndex
        , LevelType Type
        , bool IsTimed
        , float TimeLimit
        , float Star2TimeLeft
        , int QuotaCount
        , QuotaSettings Quota0
        , QuotaSettings Quota1
        , QuotaSettings Quota2
        , QuotaSettings Quota3
    )
    {
        public const int MAX_QUOTAS = LevelSnapshot.MAX_QUOTAS;

        public static LevelPreview From(LevelDefinition level, int levelIndex)
            => From(level, levelIndex, GameRulesValues.Default);

        public static LevelPreview From(LevelDefinition level, int levelIndex, in GameRulesValues rules)
        {
            var levelRules = LevelRules.Resolve(level, in rules);
            var quotas = level.Quotas;
            var quotaCount = Math.Min(quotas.Length, MAX_QUOTAS);

            return new LevelPreview(
                  level.Id
                , levelIndex
                , levelRules.Type
                , levelRules.IsTimed
                , levelRules.TimeLimit
                , levelRules.Star2TimeLeft
                , quotaCount
                , ReadQuota(quotas: quotas, index: 0)
                , ReadQuota(quotas: quotas, index: 1)
                , ReadQuota(quotas: quotas, index: 2)
                , ReadQuota(quotas: quotas, index: 3)
            );
        }

        public QuotaSettings GetQuota(int index)
        {
            return index switch {
                0 => Quota0,
                1 => Quota1,
                2 => Quota2,
                3 => Quota3,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        private static QuotaSettings ReadQuota(ReadOnlySpan<QuotaSettings> quotas, int index)
            => index < Math.Min(quotas.Length, MAX_QUOTAS) ? quotas[index] : default;
    }
}
