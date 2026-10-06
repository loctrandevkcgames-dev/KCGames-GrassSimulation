using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelSessionTests
{
    private const int CUTTABLE_CELLS = 100;

    private TestAssets _assets;
    private MessageRecorder _messages;

    [SetUp]
    public void SetUp()
    {
        _assets = new TestAssets();
        _messages = new MessageRecorder();
    }

    [TearDown]
    public void TearDown()
    {
        _messages.Dispose();
        _assets.Dispose();
    }

    [Test]
    public void Preview_DoesNotRunTimerOrAcceptHarvests()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));

        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(1f);

        Assert.That(session.State, Is.EqualTo(LevelState.Preview));
        Assert.That(session.RemainingTime, Is.EqualTo(10f));
        Assert.That(session.Objectives.GetHarvested(PlantKind.Grass), Is.Zero);
    }

    [Test]
    public void QuotaMetOnTheTickTimerRunsOut_IsSuccess()
    {
        var session = CreateStartedSession(timeLimit: 1f, failOnProtectedHits: false, Quota(PlantKind.Grass, 1));

        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(1f);

        Assert.That(session.State, Is.EqualTo(LevelState.Success));
        Assert.That(session.Result.Outcome.IsSuccess, Is.True);
        Assert.That(session.Result.TryGetStars(out var stars), Is.True);
        Assert.That(stars, Is.EqualTo(StarFlags.Goal));
        Assert.That(session.RemainingTime, Is.Zero);
    }

    [Test]
    public void RemainingMainQuota_ClampsOverHarvestAndIgnoresBonus()
    {
        var session = CreateStartedSession(
            timeLimit: 10f,
            failOnProtectedHits: false,
            Quota(PlantKind.Grass, 3),
            Quota(PlantKind.HarvestFlower, 4),
            Quota(PlantKind.LowBush, 2, isBonus: true)
        );

        for (var i = 0; i < 5; i++)
        {
            session.RecordHarvest(PlantKind.Grass, xp: 0);
        }

        session.RecordHarvest(PlantKind.HarvestFlower, xp: 0);

        Assert.That(session.Objectives.RemainingMainQuota, Is.EqualTo(3));

        session.EndTick(10f);

        Assert.That(session.Result.Outcome.TryGetValue(out LevelOutcome.TimeUp timeUp), Is.True);
        Assert.That(timeUp.RemainingQuota, Is.EqualTo(3));
    }

    [Test]
    public void TimerRunsOutWithQuotaMissing_IsTimeUp()
    {
        var session = CreateStartedSession(timeLimit: 1f, failOnProtectedHits: false, Quota(PlantKind.Grass, 2));

        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(1f);

        Assert.That(session.State, Is.EqualTo(LevelState.Failure));
        Assert.That(session.Result.Outcome.TryGetValue(out LevelOutcome.TimeUp timeUp), Is.True);
        Assert.That(timeUp.RemainingQuota, Is.EqualTo(1));
    }

    [Test]
    public void UpgradeChoice_StopsTimerUntilChosen()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 500));

        session.RecordHarvest(PlantKind.Grass, xp: 100);
        session.EndTick(1f);

        Assert.That(session.State, Is.EqualTo(LevelState.UpgradeChoice));

        session.EndTick(5f);

        Assert.That(session.RemainingTime, Is.EqualTo(9f));
        Assert.That(session.TryChooseUpgrade(0), Is.True);
        Assert.That(session.State, Is.EqualTo(LevelState.Playing));
        Assert.That(session.Growth.Stats.CutRadius, Is.EqualTo(0.8f).Within(1e-5f));
    }

    [Test]
    public void Pause_StopsTimerAndHarvests()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));

        session.Pause();
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(3f);

        Assert.That(session.RemainingTime, Is.EqualTo(10f));
        Assert.That(session.Objectives.GetHarvested(PlantKind.Grass), Is.Zero);

        session.Resume();
        session.EndTick(3f);

        Assert.That(session.RemainingTime, Is.EqualTo(7f));
    }

    [Test]
    public void ContinuousProtectedTouch_CountsOneHitUntilCooldownPasses()
    {
        var session = CreateStartedSession(timeLimit: 30f, failOnProtectedHits: true, Quota(PlantKind.Grass, 5));

        for (var i = 0; i < 20; i++)
        {
            session.RecordProtectedTouch(bed: 0);
            session.EndTick(0.1f);
        }

        Assert.That(session.Protection.Hits, Is.EqualTo(1));

        session.EndTick(1.5f);
        session.RecordProtectedTouch(bed: 0);

        Assert.That(session.Protection.Hits, Is.EqualTo(2));
    }

    [Test]
    public void FourthProtectedHit_FailsWhenLevelCountsHits()
    {
        var session = CreateStartedSession(timeLimit: 30f, failOnProtectedHits: true, Quota(PlantKind.Grass, 5));

        TouchProtectedTimes(session, 4);

        Assert.That(session.State, Is.EqualTo(LevelState.Failure));
        Assert.That(session.Result.Outcome.TryGetValue(out LevelOutcome.TooManyProtectedHits failure), Is.True);
        Assert.That(failure.Hits, Is.EqualTo(session.Protection.Hits));
        Assert.That(failure.Limit, Is.EqualTo(session.ProtectedHitLimit));
    }

    [Test]
    public void ProtectedHits_DoNotFailInWarnMode()
    {
        var session = CreateStartedSession(timeLimit: 30f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));

        TouchProtectedTimes(session, 6);

        Assert.That(session.State, Is.EqualTo(LevelState.Playing));
        Assert.That(session.Protection.Hits, Is.EqualTo(6));
    }

    [Test]
    public void ProtectedHits_DoNotFailOnboardingLevelsEvenInFailMode()
    {
        var session = CreateStartedSession(
              LevelType.Tutorial
            , timeLimit: 30f
            , failOnProtectedHits: true
            , Quota(PlantKind.Grass, 5)
        );

        TouchProtectedTimes(session, 6);

        Assert.That(session.State, Is.EqualTo(LevelState.Playing));
        Assert.That(session.Protection.Hits, Is.EqualTo(6));
    }

    [Test]
    public void TutorialLevel_IsUntimedAndNeverTimesOut()
    {
        var session = CreateStartedSession(LevelType.Tutorial, 5f, false, Quota(PlantKind.Grass, 5));

        session.EndTick(100f);

        Assert.That(session.Rules.IsTimed, Is.False);
        Assert.That(session.Rules.CanLose, Is.False);
        Assert.That(session.State, Is.EqualTo(LevelState.Playing));
        Assert.That(session.RemainingTime, Is.Zero);
    }

    [Test]
    public void RelaxLevel_IsUntimedAndNeverTimesOut()
    {
        var session = CreateStartedSession(LevelType.Relax, 5f, false, Quota(PlantKind.Grass, 5));

        session.EndTick(100f);

        Assert.That(session.State, Is.EqualTo(LevelState.Playing));
    }

    [Test]
    public void UntimedCleanWin_EarnsGoalAndCleanWithoutCheckingTime()
    {
        var session = CreateStartedSession(LevelType.Tutorial, 5f, false, Quota(PlantKind.Grass, 1));

        session.EndTick(100f);
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(0.1f);

        Assert.That(session.Result.Stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean));
    }

    [Test]
    public void UntimedWinWithHit_LosesTheCleanStar()
    {
        var session = CreateStartedSession(LevelType.Tutorial, 5f, false, Quota(PlantKind.Grass, 1));

        session.RecordProtectedTouch(bed: 0);
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(0.1f);

        Assert.That(session.Result.Stars, Is.EqualTo(StarFlags.Goal));
    }

    [Test]
    public void TimerMultiplier_ScalesTheTimeLimit()
    {
        var level = _assets.CreateLevel(timeLimit: 10f, Quota(PlantKind.Grass, 5));
        var rules = GameRulesValues.Default with { TimerMultiplier = 2f };
        var session = new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher, rules);

        Assert.That(session.RemainingTime, Is.EqualTo(20f));
        Assert.That(session.Rules.TimeLimit, Is.EqualTo(20f));
    }

    [Test]
    public void TimerDisabled_MakesEveryLevelUntimed()
    {
        var level = _assets.CreateLevel(timeLimit: 10f, Quota(PlantKind.Grass, 5));
        var rules = GameRulesValues.Default with { TimerEnabled = false };
        var session = new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher, rules);

        session.TryBegin();
        session.EndTick(100f);

        Assert.That(session.State, Is.EqualTo(LevelState.Playing));
    }

    [Test]
    public void AssistedWin_EarnsOnlyTheGoalStar()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 1));

        session.IsAssisted = true;
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(1f);

        Assert.That(session.Result.Stars, Is.EqualTo(StarFlags.Goal));
        Assert.That(session.Result.IsAssisted, Is.True);
    }

    [Test]
    public void SideStar_FallsBackToSweepingNinetyPercentWithoutASideQuota()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 1));

        for (var i = 0; i < 90; i++)
        {
            session.RecordHarvest(PlantKind.Grass, xp: 0);
        }

        session.EndTick(1f);

        Assert.That(session.Result.Stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean | StarFlags.Side));
    }

    [Test]
    public void SideStar_NeedsTheSideQuotaWhenOneExists()
    {
        var session = CreateStartedSession(
              timeLimit: 10f
            , failOnProtectedHits: false
            , Quota(PlantKind.Grass, 1)
            , Quota(PlantKind.HarvestFlower, 1, isBonus: true)
        );

        for (var i = 0; i < 95; i++)
        {
            session.RecordHarvest(PlantKind.Grass, xp: 0);
        }

        session.EndTick(1f);

        Assert.That(session.Result.Stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean));
    }

    [Test]
    public void FastCleanRunWithBonus_EarnsThreeStars()
    {
        var session = CreateStartedSession(
              timeLimit: 10f
            , failOnProtectedHits: true
            , Quota(PlantKind.Grass, 1)
            , Quota(PlantKind.HarvestFlower, 1, isBonus: true)
        );

        session.RecordHarvest(PlantKind.HarvestFlower, xp: 1);
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(1f);

        Assert.That(session.Result.TryGetStars(out var stars), Is.True);
        Assert.That(stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean | StarFlags.Side));
    }

    [Test]
    public void SlowRunWithHit_EarnsOneStar()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: true, Quota(PlantKind.Grass, 1));

        session.RecordProtectedTouch(bed: 0);
        session.EndTick(9f);
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(0.5f);

        Assert.That(session.State, Is.EqualTo(LevelState.Success));
        Assert.That(session.Result.TryGetStars(out var stars), Is.True);
        Assert.That(stars, Is.EqualTo(StarFlags.Goal));
    }

    [Test]
    public void Cleanup_ClearsAreaWithoutChangingResultOrObjectives()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 1));

        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(1f);
        var result = session.Result;

        Assert.That(session.TryEnterCleanup(), Is.True);

        session.RecordHarvest(PlantKind.Grass, xp: 500);
        session.EndTick(20f);

        Assert.That(session.State, Is.EqualTo(LevelState.Cleanup));
        Assert.That(session.Result, Is.EqualTo(result));
        Assert.That(session.Objectives.GetHarvested(PlantKind.Grass), Is.EqualTo(1));
        Assert.That(session.Growth.Xp, Is.EqualTo(1));
        Assert.That(session.ClearedFraction, Is.EqualTo(2f / CUTTABLE_CELLS));
    }

    [Test]
    public void Reset_RestoresPreviewWithSameRules()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 1));

        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(4f);
        session.Reset();

        Assert.That(session.State, Is.EqualTo(LevelState.Preview));
        Assert.That(session.RemainingTime, Is.EqualTo(10f));
        Assert.That(session.Result.IsFinished, Is.False);
        Assert.That(session.Objectives.GetHarvested(PlantKind.Grass), Is.Zero);
    }

    [Test]
    public void ObjectHarvest_CountsItsUnitsTowardTheQuota()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Melon, 2));

        session.RecordHarvest(PlantKind.Melon, xp: 8, units: 1);

        Assert.That(session.Objectives.GetHarvested(PlantKind.Melon), Is.EqualTo(1));
        Assert.That(session.Objectives.IsQuotaMet(0), Is.False);

        session.RecordHarvest(PlantKind.Melon, xp: 8, units: 1);
        session.EndTick(0.1f);

        Assert.That(_messages.QuotasCompleted, Has.Count.EqualTo(1));
        Assert.That(session.State, Is.EqualTo(LevelState.Success));
    }

    [Test]
    public void ObjectHarvest_AddsItsXpToGrowth()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Melon, 5));

        session.RecordHarvest(PlantKind.Melon, xp: 8, units: 1);
        session.RecordHarvest(PlantKind.FruitTree, xp: 0, units: 1);

        Assert.That(session.Growth.Xp, Is.EqualTo(8));
        Assert.That(session.Objectives.GetHarvested(PlantKind.FruitTree), Is.EqualTo(1));
    }

    [Test]
    public void LockedTouch_IsPublishedOncePerKindPerRun()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));

        Assert.That(session.RecordLockedTouch(PlantKind.Melon, requiredTier: 3), Is.True);
        Assert.That(session.RecordLockedTouch(PlantKind.Melon, requiredTier: 3), Is.False);
        Assert.That(session.RecordLockedTouch(PlantKind.BushBig, requiredTier: 3), Is.True);
        Assert.That(_messages.LockedTouches, Has.Count.EqualTo(2));

        session.Reset();
        session.TryBegin();

        Assert.That(session.RecordLockedTouch(PlantKind.Melon, requiredTier: 3), Is.True);
    }

    [Test]
    public void SlowHint_IsPublishedOncePerKindPerRun()
    {
        var session = CreateStartedSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));

        Assert.That(session.RecordSlowHint(PlantKind.Melon), Is.True);
        Assert.That(session.RecordSlowHint(PlantKind.Melon), Is.False);
        Assert.That(_messages.SlowHints, Has.Count.EqualTo(1));
        Assert.That(_messages.SlowHints[0].Kind, Is.EqualTo(PlantKind.Melon));
    }

    [Test]
    public void ContactFeedback_IsIgnoredBeforeTheRunStarts()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));

        Assert.That(session.RecordLockedTouch(PlantKind.Melon, requiredTier: 3), Is.False);
        Assert.That(session.RecordSlowHint(PlantKind.Melon), Is.False);
    }

    private static QuotaSettings Quota(PlantKind kind, int amount, bool isBonus = false)
        => new() { Kind = kind, Amount = amount, IsBonus = isBonus };

    private static void TouchProtectedTimes(LevelSession session, int times)
    {
        for (var i = 0; i < times; i++)
        {
            session.RecordProtectedTouch(bed: 0);
            session.EndTick(1.5f);
        }
    }

    private LevelSession CreateSession(float timeLimit, bool failOnProtectedHits, params QuotaSettings[] quotas)
        => CreateSession(LevelType.Normal, timeLimit, failOnProtectedHits, quotas);

    private LevelSession CreateSession(
          LevelType type
        , float timeLimit
        , bool failOnProtectedHits
        , params QuotaSettings[] quotas
    )
    {
        var level = _assets.CreateLevel(type, timeLimit, quotas);
        var rules = TestAssets.CreateRules(failOnProtectedHits);

        return new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher, rules);
    }

    private LevelSession CreateStartedSession(float timeLimit, bool failOnProtectedHits, params QuotaSettings[] quotas)
        => CreateStartedSession(LevelType.Normal, timeLimit, failOnProtectedHits, quotas);

    private LevelSession CreateStartedSession(
          LevelType type
        , float timeLimit
        , bool failOnProtectedHits
        , params QuotaSettings[] quotas
    )
    {
        var session = CreateSession(type, timeLimit, failOnProtectedHits, quotas);

        session.TryBegin();
        return session;
    }
}
