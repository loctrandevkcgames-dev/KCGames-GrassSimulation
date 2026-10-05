namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelResult(LevelOutcome Outcome, int Stars, float RemainingTime, int ProtectedHits);
}
