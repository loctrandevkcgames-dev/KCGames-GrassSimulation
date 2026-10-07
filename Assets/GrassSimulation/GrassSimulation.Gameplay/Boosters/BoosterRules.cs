namespace GrassSimulation.Gameplay
{
    public static class BoosterRules
    {
        /// <summary>A booster that runs for a while and changes the machine stats (Turbo; Power Blade later).</summary>
        public static bool IsTimedStat(BoosterKind kind)
            => kind == BoosterKind.Turbo;

        /// <summary>Two different timed stat boosters never run at once; Extra Time conflicts with nothing.</summary>
        public static bool Conflicts(BoosterKind first, BoosterKind second)
            => first != second && IsTimedStat(first) && IsTimedStat(second);

        public static bool IsAllowed(BoosterKind kind, in LevelRules rules)
        {
            return kind switch {
                BoosterKind.ExtraTime => rules.IsTimed,
                _ => rules.CanLose,
            };
        }
    }
}
