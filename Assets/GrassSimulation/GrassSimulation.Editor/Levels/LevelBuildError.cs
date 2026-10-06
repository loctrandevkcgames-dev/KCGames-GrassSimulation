using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Editor
{
    [PolyEnumStruct]
    public partial struct LevelBuildError
    {
        partial interface IEnumCase
        {
            string ToMessage()
                => string.Empty;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct SpecInvalid(string Id, string Reason)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The spec row '{Id}' is invalid: {Reason}";
        }

        public readonly partial record struct LayoutMissing(string Id, string Path)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"{Id} has no layout file at '{Path}'.";
        }

        public readonly partial record struct LayoutInvalid(string Id, string Reason)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"The layout of {Id} is invalid: {Reason}";
        }

        public readonly partial record struct CapacityExceeded(string Id, PlantKind Kind, int Requested, int Capacity)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"{Id} wants {Requested} {Kind} but its region holds only {Capacity}.";
        }

        public readonly partial record struct PlacementFailed(string Id, PlantKind Kind, int Placed, int Requested)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToMessage()
                => $"{Id} placed only {Placed} of {Requested} {Kind}; enlarge the region.";
        }
    }
}
