using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;

namespace GrassSimulation.Gameplay
{
    [PolyEnumStruct]
    public partial struct BoosterError
    {
        partial interface IEnumCase
        {
            string ToMessage()
                => string.Empty;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct NotEquipped(BoosterKind Kind)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The booster '{Kind}' is not equipped.";
        }

        public readonly partial record struct AlreadyUsed(BoosterKind Kind)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The booster '{Kind}' was already used in this run.";
        }

        public readonly partial record struct Blocked(BoosterKind Kind, BoosterKind By)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The booster '{Kind}' is blocked while '{By}' runs.";
        }

        public readonly partial record struct Unavailable(BoosterKind Kind)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The booster '{Kind}' cannot be used now.";
        }
    }
}
