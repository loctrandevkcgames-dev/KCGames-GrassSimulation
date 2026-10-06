using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public readonly record struct LevelSettlement(StarFlags Earned, StarFlags New, bool IsFirstCompletion);
}
