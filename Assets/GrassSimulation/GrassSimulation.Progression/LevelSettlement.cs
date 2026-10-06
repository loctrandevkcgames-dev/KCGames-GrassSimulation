namespace GrassSimulation.Progression
{
    public readonly record struct LevelSettlement(
          int CoinsGranted
        , int FirstWinCoins
        , int NewStars
        , int Stars
        , bool IsNewBest
        , bool IsFirstCompletion
    );
}
