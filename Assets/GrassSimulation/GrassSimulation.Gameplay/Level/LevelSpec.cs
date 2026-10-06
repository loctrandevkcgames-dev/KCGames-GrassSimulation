using System;

namespace GrassSimulation.Gameplay
{
    public sealed record class LevelSpec(
          string Id
        , int Order
        , int Step
        , string Name
        , string Decision
        , LevelType Type
        , int Width
        , int Length
        , int ZoneCount
        , int BedCount
        , int MaxTier
        , int[] PlantCounts
        , QuotaSettings[] Quotas
        , bool IsTimed
        , float SuggestedTimer
        , UnlockSettings Unlock
    )
    {
        public int GetCount(PlantKind kind)
        {
            var index = (int)kind;

            return index < PlantCounts.Length ? PlantCounts[index] : 0;
        }

        public bool HasSameQuotas(ReadOnlySpan<QuotaSettings> other)
        {
            var count = Quotas.Length;

            if (other.Length != count)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var expected = Quotas[i];
                var actual = other[i];

                if (expected.Kind != actual.Kind
                    || expected.Amount != actual.Amount
                    || expected.IsBonus != actual.IsBonus
                )
                {
                    return false;
                }
            }

            return true;
        }
    }
}
