using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;

namespace GrassSimulation.Gameplay
{
    [PolyEnumStruct]
    public partial struct LevelOutcome
    {
        partial interface IEnumCase
        {
            string ToLabel()
                => string.Empty;

            bool IsSuccess => false;

            bool IsTimeUp => false;

            bool IsTooManyProtectedHits => false;

            bool TryGetProtectedHits(out int hits, out int limit)
            {
                hits = 0;
                limit = 0;
                return false;
            }
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct Success(StarFlags Stars)
        {
            public bool IsSuccess => true;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToLabel()
                => $"Success, {StarRules.Count(Stars)} stars ({Stars})";
        }

        public readonly partial record struct TimeUp(int RemainingQuota)
        {
            public bool IsTimeUp => true;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToLabel()
                => $"Time up, {RemainingQuota} left";
        }

        public readonly partial record struct TooManyProtectedHits(int Hits, int Limit)
        {
            public bool IsTooManyProtectedHits => true;

            public bool TryGetProtectedHits(out int hits, out int limit)
            {
                hits = Hits;
                limit = Limit;
                return true;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToLabel()
                => $"Too many protected hits {Hits} / {Limit}";
        }
    }
}
