using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class BoosterRulesTests
{
    [Test]
    public void Conflicts_OnlyBetweenDifferentTimedStatBoosters()
    {
        Assert.That(BoosterRules.IsTimedStat(BoosterKind.Turbo), Is.True);
        Assert.That(BoosterRules.IsTimedStat(BoosterKind.ExtraTime), Is.False);

        Assert.That(BoosterRules.Conflicts(BoosterKind.Turbo, BoosterKind.Turbo), Is.False);
        Assert.That(BoosterRules.Conflicts(BoosterKind.Turbo, BoosterKind.ExtraTime), Is.False);
        Assert.That(BoosterRules.Conflicts(BoosterKind.ExtraTime, BoosterKind.Turbo), Is.False);
    }

    [Test]
    public void IsAllowed_TurboNeedsAWinnableLevelAndExtraTimeNeedsATimer()
    {
        var timed = Rules(LevelType.Normal, timerEnabled: true);
        var untimedNormal = Rules(LevelType.Normal, timerEnabled: false);
        var tutorial = Rules(LevelType.Tutorial, timerEnabled: true);
        var relax = Rules(LevelType.Relax, timerEnabled: true);

        Assert.That(BoosterRules.IsAllowed(BoosterKind.Turbo, in timed), Is.True);
        Assert.That(BoosterRules.IsAllowed(BoosterKind.ExtraTime, in timed), Is.True);

        Assert.That(BoosterRules.IsAllowed(BoosterKind.Turbo, in untimedNormal), Is.True);
        Assert.That(BoosterRules.IsAllowed(BoosterKind.ExtraTime, in untimedNormal), Is.False);

        Assert.That(BoosterRules.IsAllowed(BoosterKind.Turbo, in tutorial), Is.False);
        Assert.That(BoosterRules.IsAllowed(BoosterKind.ExtraTime, in tutorial), Is.False);
        Assert.That(BoosterRules.IsAllowed(BoosterKind.Turbo, in relax), Is.False);
        Assert.That(BoosterRules.IsAllowed(BoosterKind.ExtraTime, in relax), Is.False);
    }

    private static LevelRules Rules(LevelType type, bool timerEnabled)
    {
        var canLose = type == LevelType.Normal || type == LevelType.Hard;

        return new LevelRules(
              type
            , IsTimed: canLose && timerEnabled
            , CanLose: canLose
            , TimeLimit: 60f
            , ProtectedMode: ProtectedMode.Warn
            , FailLimit: 3
            , Retrigger: 1f
            , Star2TimeLeft: 0.2f
            , TimerWarning: 15f
        );
    }
}
