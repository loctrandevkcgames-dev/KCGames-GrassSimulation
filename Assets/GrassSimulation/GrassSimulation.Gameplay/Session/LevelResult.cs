namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelResult(LevelOutcome Outcome, float RemainingTime, int ProtectedHits)
    {
        public bool IsFinished => Outcome.GetEnumCase() != LevelOutcome.EnumCase.Undefined;

        public bool TryGetStars(out int stars)
        {
            if (Outcome.TryGetValue(out LevelOutcome.Success success))
            {
                stars = success.Stars;
                return true;
            }

            stars = 0;
            return false;
        }
    }
}
