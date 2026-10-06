namespace GrassSimulation.Gameplay
{
    public readonly record struct LevelRules(
          LevelType Type
        , bool IsTimed
        , bool CanLose
        , float TimeLimit
        , ProtectedMode ProtectedMode
        , int FailLimit
        , float Retrigger
        , float Star2TimeLeft
        , float TimerWarning
    )
    {
        public bool FailsOnProtectedHits => CanLose && ProtectedMode == ProtectedMode.Fail;

        public static LevelRules Resolve(LevelDefinition level, in GameRulesValues rules)
        {
            var type = level.Type;
            var canLose = type == LevelType.Normal || type == LevelType.Hard;
            var isTimed = canLose && rules.TimerEnabled;
            var timeLimit = isTimed ? level.TimeLimit * rules.TimerMultiplier : 0f;

            return new LevelRules(
                  type
                , isTimed
                , canLose
                , timeLimit
                , rules.ProtectedMode
                , rules.FailLimit
                , rules.Retrigger
                , rules.Star2TimeLeft
                , rules.TimerWarning
            );
        }
    }
}
