using System.Text.RegularExpressions;
using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrassSimulation.Progression.Tests;

public sealed class ProgressionServiceTests
{
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
    public void SettlingTheSameResultTwice_PaysOnce()
    {
        var first = Settle(stars: 2);
        var second = Settle(stars: 2);

        Assert.That(first.CoinsGranted, Is.EqualTo(150));
        Assert.That(second.CoinsGranted, Is.Zero);
        Assert.That(_service.Coins, Is.EqualTo(150));
        Assert.That(_store.SaveCount, Is.EqualTo(1));
    }

    [Test]
    public void Win_MarksTheLevelCompleted()
    {
        Assert.That(_service.IsCompleted(s_level), Is.False);

        var settlement = Settle(stars: 1);

        Assert.That(_service.IsCompleted(s_level), Is.True);
        Assert.That(settlement.IsFirstCompletion, Is.True);
        Assert.That(_service.IsGranted(new RewardId.FirstWin(s_level)), Is.True);
    }

    [Test]
    public void BestStars_NeverDecrease()
    {
        Settle(stars: 3);

        var settlement = Settle(stars: 1);

        Assert.That(_service.GetBestStars(s_level), Is.EqualTo(3));
        Assert.That(settlement.IsNewBest, Is.False);
    }

    [Test]
    public void ReplayWithMoreStars_RaisesBestAndPaysOnlyNewStars()
    {
        Settle(stars: 1);

        var settlement = Settle(stars: 3);

        Assert.That(_service.GetBestStars(s_level), Is.EqualTo(3));
        Assert.That(settlement.IsNewBest, Is.True);
        Assert.That(settlement.IsFirstCompletion, Is.False);
        Assert.That(settlement.CoinsGranted, Is.EqualTo(50));
        Assert.That(_service.Coins, Is.EqualTo(175));
    }

    [Test]
    public void FailingSave_RollsBackCoinsLedgerBestAndCompleted()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        var outcome = _service.Settle(s_level, LevelResults.Win(stars: 3));

        Assert.That(outcome.TryGetError(out var error), Is.True);
        Assert.That(error.GetEnumCase(), Is.EqualTo(SettleError.EnumCase.NotSaved));
        Assert.That(error.TryGetValue(out SettleError.NotSaved notSaved), Is.True);
        Assert.That(notSaved.Cause.GetEnumCase(), Is.EqualTo(SaveError.EnumCase.WriteFailed));

        AssertEmpty();

        _store.FailSaves = false;

        Assert.That(Settle(stars: 3).CoinsGranted, Is.EqualTo(175));
    }

    [Test]
    public void ThrowingStore_RollsBackAndDoesNotThrow()
    {
        _store.ThrowOnSave = true;

        LogAssert.Expect(LogType.Error, new Regex("threw during"));
        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        var outcome = _service.Settle(s_level, LevelResults.Win(stars: 3));

        Assert.That(outcome.IsError, Is.True);

        AssertEmpty();

        _store.ThrowOnSave = false;

        Assert.That(Settle(stars: 3).CoinsGranted, Is.EqualTo(175));
    }

    [Test]
    public void FailingSaveAfterAnEarlierWin_RestoresTheEarlierState()
    {
        Settle(stars: 1);

        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Assert.That(_service.Settle(s_level, LevelResults.Win(stars: 3)).IsError, Is.True);

        Assert.That(_service.Coins, Is.EqualTo(125));
        Assert.That(_service.GetBestStars(s_level), Is.EqualTo(1));
        Assert.That(_service.IsCompleted(s_level), Is.True);
        Assert.That(_service.IsGranted(new RewardId.FirstWin(s_level)), Is.True);
        Assert.That(_service.IsGranted(new RewardId.Star(s_level, 1)), Is.True);
        Assert.That(_service.IsGranted(new RewardId.Star(s_level, 2)), Is.False);
        Assert.That(_service.IsGranted(new RewardId.Star(s_level, 3)), Is.False);

        _store.FailSaves = false;

        Assert.That(Settle(stars: 3).CoinsGranted, Is.EqualTo(50));
        Assert.That(_service.Coins, Is.EqualTo(175));
    }

    [Test]
    public void Loss_ChangesNothingAndDoesNotSave()
    {
        var outcome = _service.Settle(s_level, LevelResults.Loss());

        Assert.That(outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.CoinsGranted, Is.Zero);
        Assert.That(_service.Coins, Is.Zero);
        Assert.That(_service.IsCompleted(s_level), Is.False);
        Assert.That(_store.SaveCount, Is.Zero);
    }

    [Test]
    public void Wipe_ResetsProgress()
    {
        Settle(stars: 2);

        Assert.That(_service.Wipe().IsSuccess, Is.True);

        Assert.That(_service.Coins, Is.Zero);
        Assert.That(_service.IsCompleted(s_level), Is.False);
        Assert.That(_store.Saved, Is.Null);
        Assert.That(_store.DeleteCount, Is.EqualTo(1));
    }

    [Test]
    public void FailingWipe_KeepsTheProgress()
    {
        Settle(stars: 2);

        _store.FailDeletes = true;

        Assert.That(_service.Wipe().IsSuccess, Is.False);

        Assert.That(_service.Coins, Is.EqualTo(150));
        Assert.That(_service.IsCompleted(s_level), Is.True);
        Assert.That(_store.Saved, Is.Not.Null);
    }

    [Test]
    public void NothingSaved_StartsEmptyAndWritable()
    {
        Assert.That(_service.IsReadOnly, Is.False);
        Assert.That(_service.Coins, Is.Zero);
    }

    [Test]
    public void CorruptSave_StartsEmptyAndWritable()
    {
        _store.LoadFailure = new LoadError.Corrupt("progress.json");
        _service.Reload();

        Assert.That(_service.IsReadOnly, Is.False);
        Assert.That(Settle(stars: 1).CoinsGranted, Is.EqualTo(125));
    }

    [Test]
    public void UnreadableSave_MakesTheServiceReadOnlyAndSettleReportsTheCause()
    {
        _store.LoadFailure = new LoadError.Unreadable("progress.json", "locked");
        _service.Reload();

        LogAssert.Expect(LogType.Warning, new Regex("read-only"));

        var outcome = _service.Settle(s_level, LevelResults.Win(stars: 3));

        Assert.That(_service.IsReadOnly, Is.True);
        Assert.That(_service.LoadFailure.GetEnumCase(), Is.EqualTo(LoadError.EnumCase.Unreadable));
        Assert.That(outcome.TryGetError(out var error), Is.True);
        Assert.That(error.TryGetValue(out SettleError.StoreUnavailable unavailable), Is.True);
        Assert.That(unavailable.Cause.GetEnumCase(), Is.EqualTo(LoadError.EnumCase.Unreadable));
        Assert.That(_store.SaveCount, Is.Zero);
        Assert.That(_service.Coins, Is.Zero);
    }

    [Test]
    public void ReloadAfterTheStoreRecovers_LeavesReadOnly()
    {
        _store.LoadFailure = new LoadError.Unreadable("progress.json", "locked");
        _service.Reload();
        _store.LoadFailure = null;
        _service.Reload();

        Assert.That(_service.IsReadOnly, Is.False);
        Assert.That(Settle(stars: 1).CoinsGranted, Is.EqualTo(125));
    }

    [Test]
    public void FindFirstIncomplete_ReturnsTheFirstLevelNotCompleted()
    {
        var catalog = TestCatalogs.Create("level-01", "level-02", "level-03", out var objects);

        try
        {
            Assert.That(_service.FindFirstIncomplete(catalog), Is.Zero);

            Settle(stars: 1);

            Assert.That(_service.FindFirstIncomplete(catalog), Is.EqualTo(1));

            _service.Settle(new LevelId("level-03"), LevelResults.Win(stars: 1));

            Assert.That(_service.FindFirstIncomplete(catalog), Is.EqualTo(1));

            _service.Settle(new LevelId("level-02"), LevelResults.Win(stars: 1));

            Assert.That(_service.FindFirstIncomplete(catalog), Is.EqualTo(2));
        }
        finally
        {
            TestCatalogs.DestroyAll(objects);
        }
    }

    private LevelSettlement Settle(int stars)
    {
        var outcome = _service.Settle(s_level, LevelResults.Win(stars));

        Assert.That(outcome.TryGetValue(out var settlement), Is.True);
        return settlement;
    }

    private void AssertEmpty()
    {
        Assert.That(_service.Coins, Is.Zero);
        Assert.That(_service.GetBestStars(s_level), Is.Zero);
        Assert.That(_service.IsCompleted(s_level), Is.False);
        Assert.That(_service.IsGranted(new RewardId.FirstWin(s_level)), Is.False);
        Assert.That(_service.IsGranted(new RewardId.Star(s_level, 1)), Is.False);
    }
}
