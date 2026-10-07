using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    [PolyEnumStruct]
    public partial struct SelectMachineError
    {
        partial interface IEnumCase
        {
            string ToMessage()
                => string.Empty;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct NotOwned(MachineId Machine)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The machine '{Machine.Value}' is not owned.";
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
                => $"The machine choice was not saved: {Cause.ToMessage()}";
        }
    }
}
