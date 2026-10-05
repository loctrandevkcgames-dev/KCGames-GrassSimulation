using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression.Tests;

internal static class LevelResults
{
    public static LevelResult Win(int stars)
    {
        LevelOutcome outcome = new LevelOutcome.Success(stars);

        return new LevelResult(Outcome: outcome, RemainingTime: 10f, ProtectedHits: 0);
    }

    public static LevelResult Loss()
    {
        LevelOutcome outcome = new LevelOutcome.TimeUp(RemainingQuota: 2);

        return new LevelResult(Outcome: outcome, RemainingTime: 0f, ProtectedHits: 0);
    }
}
