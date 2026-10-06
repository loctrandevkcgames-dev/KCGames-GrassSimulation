namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelValidationRules(
          float BodyRadius
        , float PathRadius
        , float BaseReach
        , float WideReach
        , float ReachPerTier
        , float BedMargin
        , int WideFromLevel
        , float XpSurplus
        , float QuotaSurplus
        , float MinTimer
        , float GridStep
        , int[] XpThresholds
    )
    {
        public static readonly LevelValidationRules Default = new(
              BodyRadius: 0.3f
            , PathRadius: 0.425f
            , BaseReach: 0.65f
            , WideReach: 0.78f
            , ReachPerTier: 0.15f
            , BedMargin: 0.1f
            , WideFromLevel: 11
            , XpSurplus: 1.2f
            , QuotaSurplus: 1.1f
            , MinTimer: 60f
            , GridStep: 0.1f
            , XpThresholds: new[] { 100, 260, 480 }
        );

        public float MaxBladeReach(int maxTier, int order)
        {
            var baseReach = order >= WideFromLevel ? WideReach : BaseReach;

            return baseReach + ReachPerTier * (maxTier - 1);
        }
    }
}
