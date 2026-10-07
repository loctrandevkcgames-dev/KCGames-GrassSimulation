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
    public void NewSave_OwnsOnlyTheStandardMachine()
    {
        Assert.That(_service.OwnedMachineCount, Is.EqualTo(1));
        Assert.That(_service.IsMachineOwned(MachineIds.Standard), Is.True);
        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.False);
        Assert.That(_service.SelectedMachine, Is.EqualTo(MachineIds.Standard));
    }

    [Test]
    public void WinningTheWideLevel_UnlocksTheWideMachineOnce()
    {
        var first = _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), WideUnlock());

        Assert.That(first.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Unlocks, Has.Length.EqualTo(1));
        Assert.That(settlement.Unlocks[0].Kind, Is.EqualTo(UnlockKind.WideMachine));
        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.True);
        Assert.That(_service.OwnedMachineCount, Is.EqualTo(2));
        Assert.That(_service.IsUnlockGranted(s_level, UnlockKind.WideMachine), Is.True);

        var second = _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), WideUnlock());

        Assert.That(second.TryGetValue(out var again), Is.True);
        Assert.That(again.Unlocks, Is.Empty);
        Assert.That(_service.OwnedMachineCount, Is.EqualTo(2));
    }

    [Test]
    public void LosingTheWideLevel_GrantsNothing()
    {
        var outcome = _service.Settle(s_level, LevelResults.Loss(), WideUnlock());

        Assert.That(outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Unlocks, Is.Empty);
        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.False);
        Assert.That(_service.IsUnlockGranted(s_level, UnlockKind.WideMachine), Is.False);
    }

    [Test]
    public void BoosterUnlock_IsRecordedAsGrantedWithoutOwningAMachine()
    {
        var unlock = new UnlockSettings { Kind = UnlockKind.TurboBooster, Amount = 3 };

        var outcome = _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), unlock);

        Assert.That(outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Unlocks, Has.Length.EqualTo(1));
        Assert.That(_service.IsUnlockGranted(s_level, UnlockKind.TurboBooster), Is.True);
        Assert.That(_service.OwnedMachineCount, Is.EqualTo(1));
    }

    [Test]
    public void FailedSave_RollsBackTheUnlock()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        var outcome = _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), WideUnlock());

        Assert.That(outcome.IsError, Is.True);
        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.False);
        Assert.That(_service.IsUnlockGranted(s_level, UnlockKind.WideMachine), Is.False);
    }

    [Test]
    public void SelectMachine_ChoosesAnOwnedMachineAndPersistsIt()
    {
        _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), WideUnlock());

        var selected = _service.SelectMachine(MachineIds.Wide);

        Assert.That(selected.IsSuccess, Is.True);
        Assert.That(_service.SelectedMachine, Is.EqualTo(MachineIds.Wide));
        Assert.That(_store.Saved.SelectedMachine, Is.EqualTo(MachineIds.Wide.Value));
    }

    [Test]
    public void SelectMachine_RejectsAMachineThatIsNotOwned()
    {
        var selected = _service.SelectMachine(MachineIds.Wide);

        Assert.That(selected.IsSuccess, Is.False);
        Assert.That(selected.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.ToMessage(), Does.Contain("not owned"));
        Assert.That(_service.SelectedMachine, Is.EqualTo(MachineIds.Standard));
    }

    [Test]
    public void SelectMachine_FailedSaveKeepsTheOldSelection()
    {
        _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), WideUnlock());
        _store.FailSaves = true;

        var selected = _service.SelectMachine(MachineIds.Wide);

        Assert.That(selected.IsSuccess, Is.False);
        Assert.That(_service.SelectedMachine, Is.EqualTo(MachineIds.Standard));
    }

    [Test]
    public void SelectedMachine_SurvivesAReload()
    {
        _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), WideUnlock());
        _service.SelectMachine(MachineIds.Wide);

        var reloaded = new ProgressionService(_store);

        reloaded.Initialize();

        Assert.That(reloaded.SelectedMachine, Is.EqualTo(MachineIds.Wide));
        Assert.That(reloaded.IsMachineOwned(MachineIds.Wide), Is.True);
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

    [Test]
    public void BoosterUnlock_GrantsTheGiftOnceAndReportsTheGrantedAmount()
    {
        var first = _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), BoosterUnlock(UnlockKind.TurboBooster));

        Assert.That(first.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Unlocks, Has.Length.EqualTo(1));
        Assert.That(settlement.Unlocks[0].Amount, Is.EqualTo(3));
        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(3));
        Assert.That(_service.HasBoosterStock, Is.True);

        var second = _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), BoosterUnlock(UnlockKind.TurboBooster));

        Assert.That(second.TryGetValue(out var again), Is.True);
        Assert.That(again.Unlocks, Is.Empty);
        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(3));
    }

    [Test]
    public void BoosterGift_FollowsTheConfiguredRules()
    {
        var rules = GameRulesValues.Default with { BoosterGiftTurbo = 5, BoosterGiftExtraTime = 2 };
        var service = new ProgressionService(new FakeProgressStore(), rules);
        var second = new LevelId("level-02");

        service.Initialize();
        service.Settle(s_level, LevelResults.Win(StarFlags.Goal), BoosterUnlock(UnlockKind.TurboBooster));
        service.Settle(second, LevelResults.Win(StarFlags.Goal), BoosterUnlock(UnlockKind.ExtraTimeBooster));

        Assert.That(service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(5));
        Assert.That(service.GetBoosterStock(BoosterKind.ExtraTime), Is.EqualTo(2));
    }

    [Test]
    public void LosingTheBoosterLevel_GrantsNoGift()
    {
        _service.Settle(s_level, LevelResults.Loss(), BoosterUnlock(UnlockKind.TurboBooster));

        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.Zero);
        Assert.That(_service.HasBoosterStock, Is.False);
    }

    [Test]
    public void FailedSettle_RollsTheGiftBack()
    {
        _store.FailSaves = true;
        LogAssert.Expect(LogType.Error, new Regex("was not saved"));

        _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), BoosterUnlock(UnlockKind.TurboBooster));

        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.Zero);
        Assert.That(_service.IsUnlockGranted(s_level, UnlockKind.TurboBooster), Is.False);
    }

    [Test]
    public void ConsumeBooster_SpendsOneAndSavesAtOnce()
    {
        GrantTurbo();

        var saves = _store.SaveCount;
        var consumed = _service.ConsumeBooster(BoosterKind.Turbo);

        Assert.That(consumed.IsSuccess, Is.True);
        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(2));
        Assert.That(_store.SaveCount, Is.EqualTo(saves + 1));
        Assert.That(_store.Saved.BoosterStock["Turbo"], Is.EqualTo(2));
    }

    [Test]
    public void ConsumeBooster_WithoutStock_ChangesNothing()
    {
        var saves = _store.SaveCount;
        var consumed = _service.ConsumeBooster(BoosterKind.ExtraTime);

        Assert.That(consumed.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterStockError.NotInStock _), Is.True);
        Assert.That(_store.SaveCount, Is.EqualTo(saves));
    }

    [Test]
    public void ConsumeBooster_WhenTheSaveFails_StaysSpentAndReportsIt()
    {
        GrantTurbo();
        _store.FailSaves = true;

        var consumed = _service.ConsumeBooster(BoosterKind.Turbo);

        Assert.That(consumed.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterStockError.NotSaved _), Is.True);
        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(2));
    }

    [Test]
    public void ConsumingTheLastBooster_UnequipsItSoItIsNotAutoEquippedAgain()
    {
        GrantTurbo();
        _service.SetBoosterEquipped(BoosterKind.Turbo, isEquipped: true);

        _service.ConsumeBooster(BoosterKind.Turbo);
        _service.ConsumeBooster(BoosterKind.Turbo);

        Assert.That(_service.IsBoosterEquipped(BoosterKind.Turbo), Is.True);

        _service.ConsumeBooster(BoosterKind.Turbo);

        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.Zero);
        Assert.That(_service.IsBoosterEquipped(BoosterKind.Turbo), Is.False);
        Assert.That(_store.Saved.EquippedBoosters, Is.Empty);
    }

    [Test]
    public void Equipping_NeedsStockAndDoesNotConsumeIt()
    {
        var withoutStock = _service.SetBoosterEquipped(BoosterKind.Turbo, isEquipped: true);

        Assert.That(withoutStock.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterStockError.NotInStock _), Is.True);

        GrantTurbo();

        Assert.That(_service.SetBoosterEquipped(BoosterKind.Turbo, isEquipped: true).IsSuccess, Is.True);
        Assert.That(_service.IsBoosterEquipped(BoosterKind.Turbo), Is.True);
        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(3));

        Assert.That(_service.SetBoosterEquipped(BoosterKind.Turbo, isEquipped: false).IsSuccess, Is.True);
        Assert.That(_service.IsBoosterEquipped(BoosterKind.Turbo), Is.False);
    }

    [Test]
    public void Equipping_WhenTheSaveFails_RollsBack()
    {
        GrantTurbo();
        _store.FailSaves = true;

        var equipped = _service.SetBoosterEquipped(BoosterKind.Turbo, isEquipped: true);

        Assert.That(equipped.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterStockError.NotSaved _), Is.True);
        Assert.That(_service.IsBoosterEquipped(BoosterKind.Turbo), Is.False);
    }

    [Test]
    public void ConsumeBooster_AfterAStoreLoadFailure_IsRejected()
    {
        var store = new FakeProgressStore { LoadFailure = new LoadError.Unreadable("fake", "locked") };
        var service = new ProgressionService(store);

        service.Initialize();

        var consumed = service.ConsumeBooster(BoosterKind.Turbo);

        Assert.That(consumed.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterStockError.StoreUnavailable _), Is.True);
    }

    private void GrantTurbo()
    {
        _service.Settle(s_level, LevelResults.Win(StarFlags.Goal), BoosterUnlock(UnlockKind.TurboBooster));
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

    private static UnlockSettings BoosterUnlock(UnlockKind kind)
        => new() { Kind = kind, Amount = 0 };

    private static UnlockSettings WideUnlock()
        => new() { Kind = UnlockKind.WideMachine, Amount = 0 };
}
