using System.Runtime.CompilerServices;
using EncosyTower.Common;
using EncosyTower.TypeWraps;

namespace GrassSimulation.Gameplay
{
    [WrapRecord]
    public readonly partial record struct LevelId(string Value) : IIsValid
    {
        public bool IsValid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => string.IsNullOrEmpty(Value) == false;
        }
    }
}
