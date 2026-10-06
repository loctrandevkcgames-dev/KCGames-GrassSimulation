using System.Text.RegularExpressions;
using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrassSimulation.Progression.Tests;

public sealed class ProgressionServiceTests
{
    private const StarFlags GOAL_CLEAN = StarFlags.Goal | StarFlags.Clean;

    private static readonly LevelId s_level = new("level-01");

    private FakeProgressStore _store;
    private ProgressionService _service;

    [SetUp]
    public void SetUp()
    {
        _store = new FakeProgressStore();
        _service = new ProgressionService(_store);
        _service.Initialize();
    }

    [Test]
    public void FirstWin_EarnsTheStarsAndMarksTheFirstCompletion()
    {
        var settlement = Settle(GOAL_CLEAN);

        Assert.That(settlement.Earned, Is.EqualTo(GOAL_CLEAN));
        Assert.That(settlement.New, Is.EqualTo(GOAL_CLEAN));
        Assert.That(settlement.IsFirstCompletion, Is.True);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(GOAL_CLEAN));
        Assert.That(_service.GetStarCount(s_level), Is.EqualTo(2));
        Assert.That(_store.SaveCount, Is.EqualTo(1));
    }

    [Test]
    public void SettlingTheSameResultTwice_ReportsNoNewStars()
    {
        Settle(GOAL_CLEAN);

        var second = Settle(GOAL_CLEAN);

        Assert.That(second.Earned, Is.EqualTo(GOAL_CLEAN));
        Assert.That(second.New, Is.EqualTo(StarFlags.None));
        Assert.That(second.IsFirstCompletion, Is.False);
        Assert.That(_service.GetAttempts(s_level), Is.EqualTo(2));
    }

    [Test]
    public void Win_MarksTheLevelCompleted()
    {
        Assert.That(_service.IsCompleted(s_level), Is.False);

        Settle(StarFlags.Goal);

        Assert.That(_service.IsCompleted(s_level), Is.True);
    }

    [Test]
    public void Stars_AreOrMergedAcrossRunsAndNeverLost()
    {
        Settle(StarFlags.Goal | StarFlags.Side);

        var settlement = Settle(GOAL_CLEAN);

        Assert.That(settlement.New, Is.EqualTo(StarFlags.Clean));
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarRules.ALL));

        Settle(StarFlags.Goal);

        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarRules.ALL));
    }

    [Test]
    public void AssistedResult_KeepsTheGoalOnly()
    {
        var settlement = Settle(StarFlags.Goal);

        Assert.That(settlement.Earned, Is.EqualTo(StarFlags.Goal));
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.Goal));
    }

    [Test]
    public void FailingSave_RollsBackStarsAttemptsAndCompleted()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        var outcome = _service.Settle(s_level, LevelResults.Win(StarRules.ALL));

        Assert.That(outcome.TryGetError(out var error), Is.True);
        Assert.That(error.GetEnumCase(), Is.EqualTo(SettleError.EnumCase.NotSaved));
        Assert.That(error.CanRetry, Is.True);
        Assert.That(error.TryGetValue(out SettleError.NotSaved notSaved), Is.True);
        Assert.That(notSaved.Cause.GetEnumCase(), Is.EqualTo(SaveError.EnumCase.WriteFailed));

        AssertEmpty();

        _store.FailSaves = false;

        Assert.That(Settle(StarRules.ALL).New, Is.EqualTo(StarRules.ALL));
    }

    [Test]
    public void ThrowingStore_RollsBackAndDoesNotThrow()
    {
        _store.ThrowOnSave = true;

        LogAssert.Expect(LogType.Error, new Regex("threw during"));
        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        var outcome = _service.Settle(s_level, LevelResults.Win(StarRules.ALL));

        Assert.That(outcome.IsError, Is.True);

        AssertEmpty();

        _store.ThrowOnSave = false;

        Assert.That(Settle(StarRules.ALL).New, Is.EqualTo(StarRules.ALL));
    }

    [Test]
    public void FailingSaveAfterAnEarlierWin_RestoresTheEarlierState()
    {
        Settle(StarFlags.Goal);

        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Assert.That(_service.Settle(s_level, LevelResults.Win(StarRules.ALL)).IsError, Is.True);

        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.Goal));
        Assert.That(_service.GetAttempts(s_level), Is.EqualTo(1));
        Assert.That(_service.IsCompleted(s_level), Is.True);

        _store.FailSaves = false;

        Assert.That(Settle(StarRules.ALL).New, Is.EqualTo(StarFlags.Clean | StarFlags.Side));
    }

    [Test]
    public void Loss_CountsAnAttemptAndEarnsNothing()
    {
        var outcome = _service.Settle(s_level, LevelResults.Loss());

        Assert.That(outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Earned, Is.EqualTo(StarFlags.None));
        Assert.That(settlement.New, Is.EqualTo(StarFlags.None));
        Assert.That(settlement.IsFirstCompletion, Is.False);
        Assert.That(_service.GetAttempts(s_level), Is.EqualTo(1));
        Assert.That(_service.IsCompleted(s_level), Is.False);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
    }

    [Test]
    public void LossAfterAWin_KeepsTheStarsAndCompletion()
    {
        Settle(StarRules.ALL);

        _service.Settle(s_level, LevelResults.Loss());

        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarRules.ALL));
        Assert.That(_service.IsCompleted(s_level), Is.True);
        Assert.That(_service.GetAttempts(s_level), Is.EqualTo(2));
    }

    [Test]
    public void Wipe_ResetsProgress()
    {
        Settle(GOAL_CLEAN);

        Assert.That(_service.Wipe().IsSuccess, Is.True);

        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
        Assert.That(_service.IsCompleted(s_level), Is.False);
        Assert.That(_store.Saved, Is.Null);
        Assert.That(_store.DeleteCount, Is.EqualTo(1));
    }

    [Test]
    public void FailingWipe_KeepsTheProgress()
    {
        Settle(GOAL_CLEAN);

        _store.FailDeletes = true;

        Assert.That(_service.Wipe().IsSuccess, Is.False);

        Assert.That(_service.GetStars(s_level), Is.EqualTo(GOAL_CLEAN));
        Assert.That(_service.IsCompleted(s_level), Is.True);
        Assert.That(_store.Saved, Is.Not.Null);
    }

    [Test]
    public void NothingSaved_StartsEmptyAndWritable()
    {
        Assert.That(_service.IsReadOnly, Is.False);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
    }

    [Test]
    public void CorruptSave_StartsEmptyAndWritable()
    {
        _store.LoadFailure = new LoadError.Corrupt("progress.json");
        _service.Reload();

        Assert.That(_service.IsReadOnly, Is.False);
        Assert.That(Settle(StarFlags.Goal).IsFirstCompletion, Is.True);
    }

    [Test]
    public void UnreadableSave_MakesTheServiceReadOnlyAndSettleReportsTheCause()
    {
        _store.LoadFailure = new LoadError.Unreadable("progress.json", "locked");
        _service.Reload();

        LogAssert.Expect(LogType.Warning, new Regex("read-only"));

        var outcome = _service.Settle(s_level, LevelResults.Win(StarRules.ALL));

        Assert.That(_service.IsReadOnly, Is.True);
        Assert.That(_service.LoadFailure.GetEnumCase(), Is.EqualTo(LoadError.EnumCase.Unreadable));
        Assert.That(outcome.TryGetError(out var error), Is.True);
        Assert.That(error.CanRetry, Is.False);
        Assert.That(error.TryGetValue(out SettleError.StoreUnavailable unavailable), Is.True);
        Assert.That(unavailable.Cause.GetEnumCase(), Is.EqualTo(LoadError.EnumCase.Unreadable));
        Assert.That(_store.SaveCount, Is.Zero);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
    }

    [Test]
    public void ReloadAfterTheStoreRecovers_LeavesReadOnly()
    {
        _store.LoadFailure = new LoadError.Unreadable("progress.json", "locked");
        _service.Reload();
        _store.LoadFailure = null;
        _service.Reload();

        Assert.That(_service.IsReadOnly, Is.False);
        Assert.That(Settle(StarFlags.Goal).IsFirstCompletion, Is.True);
    }

    [Test]
    public void FindFirstIncomplete_ReturnsTheFirstLevelNotCompleted()
    {
        var catalog = TestCatalogs.Create("level-01", "level-02", "level-03", out var objects);

        try
        {
            Assert.That(_service.FindFirstIncomplete(catalog), Is.Zero);

            Settle(StarFlags.Goal);

            Assert.That(_service.FindFirstIncomplete(catalog), Is.EqualTo(1));

            _service.Settle(new LevelId("level-03"), LevelResults.Win(StarFlags.Goal));

            Assert.That(_service.FindFirstIncomplete(catalog), Is.EqualTo(1));

            _service.Settle(new LevelId("level-02"), LevelResults.Win(StarFlags.Goal));

            Assert.That(_service.FindFirstIncomplete(catalog), Is.EqualTo(2));
        }
        finally
        {
            TestCatalogs.DestroyAll(objects);
        }
    }

    private LevelSettlement Settle(StarFlags stars)
    {
        var outcome = _service.Settle(s_level, LevelResults.Win(stars));

        Assert.That(outcome.TryGetValue(out var settlement), Is.True);
        return settlement;
    }

    private void AssertEmpty()
    {
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
        Assert.That(_service.GetAttempts(s_level), Is.Zero);
        Assert.That(_service.IsCompleted(s_level), Is.False);
    }
}
