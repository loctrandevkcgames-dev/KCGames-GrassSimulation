using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    [PolyEnumStruct]
    public partial struct BoosterStockError
    {
        partial interface IEnumCase
        {
            string ToMessage()
                => string.Empty;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct NotInStock(BoosterKind Kind)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The booster '{Kind}' is out of stock.";
        }

        public readonly partial record struct StoreUnavailable(LoadError Cause)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The progress store is unavailable: {Cause.ToMessage()}";
        }

        public readonly partial record struct NotSaved(SaveError Cause)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The booster stock was not saved: {Cause.ToMessage()}";
        }
    }
}
