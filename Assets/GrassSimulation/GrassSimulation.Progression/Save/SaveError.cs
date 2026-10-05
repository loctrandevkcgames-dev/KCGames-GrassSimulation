using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;

namespace GrassSimulation.Progression
{
    [PolyEnumStruct]
    public partial struct SaveError
    {
        partial interface IEnumCase
        {
            string ToMessage()
                => string.Empty;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial struct SerializeFailed
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => "The progress save cannot be serialized";
        }

        public readonly partial record struct WriteFailed(string Path, string Reason)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The progress save '{Path}' cannot be written: {Reason}";
        }
    }
}
