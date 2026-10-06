namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelResult(
          LevelOutcome Outcome
        , float RemainingTime
        , int ProtectedHits
        , bool IsAssisted = false
    )
    {
        public bool IsFinished => Outcome.GetEnumCase() != LevelOutcome.EnumCase.Undefined;

        public StarFlags Stars
            => Outcome.TryGetValue(out LevelOutcome.Success success) ? success.Stars : StarFlags.None;

        public bool TryGetStars(out StarFlags stars)
        {
            stars = Stars;
            return Outcome.IsSuccess;
        }
    }
}
