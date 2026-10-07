using System;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public readonly record struct LevelSettlement(
          StarFlags Earned
        , StarFlags New
        , bool IsFirstCompletion
        , UnlockSettings[] Unlocks
    )
    {
        public LevelSettlement(StarFlags Earned, StarFlags New, bool IsFirstCompletion)
            : this(Earned, New, IsFirstCompletion, Array.Empty<UnlockSettings>())
        {
        }
    }
}
