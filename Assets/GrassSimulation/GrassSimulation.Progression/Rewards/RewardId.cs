using System.Runtime.CompilerServices;
using EncosyTower.PolyEnumStructs;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    [PolyEnumStruct]
    public partial struct RewardId
    {
        partial interface IEnumCase
        {
            string ToKey()
                => string.Empty;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct FirstWin(LevelId Level)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToKey()
                => $"first-win:{Level.Value}";
        }

        public readonly partial record struct Star(LevelId Level, int Number)
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToKey()
                => $"star:{Level.Value}:{Number}";
        }
    }
}
