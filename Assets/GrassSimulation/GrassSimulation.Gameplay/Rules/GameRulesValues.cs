namespace GrassSimulation.Gameplay
{
    public readonly record struct GameRulesValues(
          ProtectedMode ProtectedMode
        , int FailLimit
        , float Retrigger
        , bool TimerEnabled
        , float TimerMultiplier
        , float Star2TimeLeft
        , float TimerWarning
        , bool AutoSlow
        , float SlowHintThreshold
        , float CleanupFinishArea
        , int CleanupClusterMax
        , float IdleHint
        , int BoosterGiftTurbo
        , int BoosterGiftExtraTime
    )
    {
        public const float DEFAULT_TIMER_WARNING = 15f;

        public static readonly GameRulesValues Default = new(
              ProtectedMode: ProtectedMode.Warn
            , FailLimit: 3
            , Retrigger: 1f
            , TimerEnabled: true
            , TimerMultiplier: 1f
            , Star2TimeLeft: 0.2f
            , TimerWarning: DEFAULT_TIMER_WARNING
            , AutoSlow: false
            , SlowHintThreshold: 0.8f
            , CleanupFinishArea: 0.01f
            , CleanupClusterMax: 4
            , IdleHint: 8f
            , BoosterGiftTurbo: 3
            , BoosterGiftExtraTime: 3
        );
    }
}
