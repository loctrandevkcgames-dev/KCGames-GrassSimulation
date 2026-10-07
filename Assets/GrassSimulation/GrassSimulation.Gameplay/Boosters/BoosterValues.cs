namespace GrassSimulation.Gameplay
{
    public readonly record struct BoosterValues(
          float TurboSeconds
        , float TurboSpeedMultiplier
        , float TurboPowerMultiplier
        , float ExtraTimeSeconds
    )
    {
        public static readonly BoosterValues Default = new(
              TurboSeconds: 8f
            , TurboSpeedMultiplier: 1.25f
            , TurboPowerMultiplier: 1.5f
            , ExtraTimeSeconds: 15f
        );
    }
}
