using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;

namespace GrassSimulation.Progression
{
    [PolyEnumStruct]
    public partial struct SettleError
    {
        partial interface IEnumCase
        {
            string ToMessage()
                => string.Empty;

            bool CanRetry => false;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct StoreUnavailable(LoadError Cause)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The progress store is unavailable: {Cause.ToMessage()}";
        }

        public readonly partial record struct NotSaved(SaveError Cause)
        {
            public bool CanRetry => true;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The settlement was not saved: {Cause.ToMessage()}";
        }
    }
}
