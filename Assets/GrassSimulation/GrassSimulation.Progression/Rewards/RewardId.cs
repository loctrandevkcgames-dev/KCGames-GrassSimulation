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

            bool IsFirstWin => false;

            bool IsStar => false;
        }

        public readonly partial struct Undefined
        {
        }

        public readonly partial record struct FirstWin(LevelId Level)
        {
            public bool IsFirstWin => true;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToKey()
                => $"first-win:{Level.Value}";
        }

        public readonly partial record struct Star(LevelId Level, int Number)
        {
            public bool IsStar => true;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public string ToKey()
                => $"star:{Level.Value}:{Number}";
        }
    }
}
