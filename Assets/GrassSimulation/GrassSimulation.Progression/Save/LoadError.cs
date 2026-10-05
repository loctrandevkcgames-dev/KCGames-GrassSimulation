using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;

namespace GrassSimulation.Progression
{
    [PolyEnumStruct]
    public partial struct LoadError
    {
        partial interface IEnumCase
        {
            bool BlocksWrites => false;

            string ToMessage()
                => string.Empty;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial struct NotFound
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => "No progress save exists";
        }

        public readonly partial record struct Unreadable(string Path, string Reason)
        {
            public bool BlocksWrites => true;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The progress save '{Path}' cannot be read: {Reason}";
        }

        public readonly partial record struct Corrupt(string Path)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The progress save '{Path}' is corrupt";
        }
    }
}
