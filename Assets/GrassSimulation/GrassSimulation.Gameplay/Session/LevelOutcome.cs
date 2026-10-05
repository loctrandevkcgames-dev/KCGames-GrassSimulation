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
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct Success(int Stars)
        {
            public bool IsSuccess => true;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToLabel()
                => $"Success, {Stars} stars";
        }

        public readonly partial record struct TimeUp(int RemainingQuota)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToLabel()
                => $"Time up, {RemainingQuota} left";
        }

        public readonly partial record struct TooManyProtectedHits(int Hits, int Limit)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToLabel()
                => $"Too many protected hits {Hits} / {Limit}";
        }
    }
}
