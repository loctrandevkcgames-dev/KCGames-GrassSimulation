using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelSessionBoosterTests
{
    private const int CUTTABLE_CELLS = 100;
    private const float TIME_LIMIT = 60f;

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
    public void Activate_InPreview_IsUnavailableAndDoesNotAssistTheRun()
    {
        var session = CreateSession(LevelType.Normal);

        session.TryEquipBooster(BoosterKind.Turbo, isEquipped: true);

        var activated = session.TryActivateBooster(BoosterKind.Turbo);

        Assert.That(activated.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterError.Unavailable _), Is.True);
        Assert.That(session.IsAssisted, Is.False);
        Assert.That(session.Boosters.IsUsed(BoosterKind.Turbo), Is.False);
        Assert.That(_messages.BoostersActivated, Is.Empty);
    }

    [Test]
    public void Equip_IsOnlyAllowedInPreviewAndLoadout()
    {
        var session = CreateSession(LevelType.Normal);

        Assert.That(session.TryEquipBooster(BoosterKind.Turbo, isEquipped: true), Is.True);

        session.TryOpenLoadout();

        Assert.That(session.TryEquipBooster(BoosterKind.ExtraTime, isEquipped: true), Is.True);

        session.TryBegin();

        Assert.That(session.TryEquipBooster(BoosterKind.Turbo, isEquipped: false), Is.False);
        Assert.That(session.Boosters.IsEquipped(BoosterKind.Turbo), Is.True);
    }

    [Test]
    public void Turbo_RaisesSpeedAndPowerButNeverTheTierOrRadius()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo);
        var before = session.Stats;
        var tier = session.Growth.Tier;

        Assert.That(session.TryActivateBooster(BoosterKind.Turbo).IsSuccess, Is.True);

        var boosted = session.Stats;

        Assert.That(boosted.Speed, Is.EqualTo(before.Speed * 1.25f).Within(1e-4f));
        Assert.That(boosted.CuttingPower, Is.EqualTo(before.CuttingPower * 1.5f).Within(1e-4f));
        Assert.That(boosted.CutRadius, Is.EqualTo(before.CutRadius));
        Assert.That(session.Growth.Tier, Is.EqualTo(tier));
        Assert.That(session.Growth.Stats, Is.EqualTo(before));
        Assert.That(session.IsAssisted, Is.True);
        Assert.That(_messages.BoostersActivated, Is.EqualTo(new[] { Activated(BoosterKind.Turbo, seconds: 8f) }));
    }

    [Test]
    public void Turbo_EndsAfterEightSecondsOfPlay()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo);

        session.TryActivateBooster(BoosterKind.Turbo);
        session.EndTick(7.9f);

        Assert.That(session.Boosters.IsRunning(BoosterKind.Turbo), Is.True);
        Assert.That(_messages.BoostersEnded, Is.Empty);

        session.EndTick(0.2f);

        Assert.That(session.Boosters.IsRunning(BoosterKind.Turbo), Is.False);
        Assert.That(_messages.BoostersEnded, Is.EqualTo(new[] { new BoosterEndedMsg(BoosterKind.Turbo) }));
        Assert.That(session.Stats, Is.EqualTo(session.Growth.Stats));
    }

    [Test]
    public void ExtraTime_AddsFifteenSecondsAtOnceAndAssistsTheRun()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.ExtraTime);

        session.EndTick(10f);

        Assert.That(session.TryActivateBooster(BoosterKind.ExtraTime).IsSuccess, Is.True);
        Assert.That(session.RemainingTime, Is.EqualTo(TIME_LIMIT - 10f + 15f).Within(1e-4f));
        Assert.That(session.IsAssisted, Is.True);
        Assert.That(_messages.BoostersActivated, Is.EqualTo(new[] { Activated(BoosterKind.ExtraTime, seconds: 15f) }));
    }

    [Test]
    public void ExtraTime_WorksWhileTurboRuns()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo, BoosterKind.ExtraTime);

        session.TryActivateBooster(BoosterKind.Turbo);

        Assert.That(session.TryActivateBooster(BoosterKind.ExtraTime).IsSuccess, Is.True);
        Assert.That(session.Boosters.IsRunning(BoosterKind.Turbo), Is.True);
    }

    [Test]
    public void ActivatingTheSameTypeAgain_ChangesNothing()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.ExtraTime);

        session.TryActivateBooster(BoosterKind.ExtraTime);

        var remaining = session.RemainingTime;
        var second = session.TryActivateBooster(BoosterKind.ExtraTime);

        Assert.That(second.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterError.AlreadyUsed _), Is.True);
        Assert.That(session.RemainingTime, Is.EqualTo(remaining));
        Assert.That(_messages.BoostersActivated, Has.Count.EqualTo(1));
    }

    [Test]
    public void PausingFreezesTheTurboDuration()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo);

        session.TryActivateBooster(BoosterKind.Turbo);
        session.EndTick(2f);
        session.Pause();
        session.EndTick(30f);

        Assert.That(session.Boosters.GetRemaining(BoosterKind.Turbo), Is.EqualTo(6f).Within(1e-4f));

        session.Resume();
        session.EndTick(1f);

        Assert.That(session.Boosters.GetRemaining(BoosterKind.Turbo), Is.EqualTo(5f).Within(1e-4f));
    }

    [Test]
    public void TheUpgradeChoiceFreezesTheTurboDuration()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo);

        session.TryActivateBooster(BoosterKind.Turbo);
        session.RecordHarvest(PlantKind.Grass, xp: 100);
        session.EndTick(1f);

        Assert.That(session.State, Is.EqualTo(LevelState.UpgradeChoice));

        var remaining = session.Boosters.GetRemaining(BoosterKind.Turbo);

        session.EndTick(5f);

        Assert.That(session.Boosters.GetRemaining(BoosterKind.Turbo), Is.EqualTo(remaining));
        Assert.That(session.TryActivateBooster(BoosterKind.ExtraTime).IsSuccess, Is.False);
    }

    [Test]
    public void Finishing_ClearsRunningEffects()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo);

        session.TryActivateBooster(BoosterKind.Turbo);
        session.EndTick(TIME_LIMIT);

        Assert.That(session.State, Is.EqualTo(LevelState.Failure));
        Assert.That(session.Boosters.IsRunning(BoosterKind.Turbo), Is.False);
        Assert.That(session.Stats, Is.EqualTo(session.Growth.Stats));
        Assert.That(_messages.BoostersEnded, Is.EqualTo(new[] { new BoosterEndedMsg(BoosterKind.Turbo) }));
    }

    [Test]
    public void ActivatingAfterTheResult_IsRejected()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.ExtraTime);

        session.EndTick(TIME_LIMIT);

        var activated = session.TryActivateBooster(BoosterKind.ExtraTime);

        Assert.That(session.State, Is.EqualTo(LevelState.Failure));
        Assert.That(activated.IsSuccess, Is.False);
        Assert.That(session.IsAssisted, Is.False);
    }

    [Test]
    public void AssistedWin_EarnsOnlyTheGoalStar()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo);

        session.TryActivateBooster(BoosterKind.Turbo);
        session.RecordHarvest(PlantKind.Grass, xp: 1, units: 5);
        session.EndTick(0.1f);

        Assert.That(session.State, Is.EqualTo(LevelState.Success));
        Assert.That(session.Result.IsAssisted, Is.True);
        Assert.That(session.Result.TryGetStars(out var stars), Is.True);
        Assert.That(stars, Is.EqualTo(StarFlags.Goal));
        Assert.That(session.Boosters.IsRunning(BoosterKind.Turbo), Is.False);
    }

    [Test]
    public void UnassistedWin_KeepsTheCleanStar()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo);

        session.RecordHarvest(PlantKind.Grass, xp: 1, units: 5);
        session.EndTick(0.1f);

        Assert.That(session.Result.IsAssisted, Is.False);
        Assert.That(session.Result.Stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean));
    }

    [Test]
    public void EnteringCleanup_ClearsEffectsAndBlocksActivation()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo, BoosterKind.ExtraTime);

        session.TryActivateBooster(BoosterKind.Turbo);
        session.RecordHarvest(PlantKind.Grass, xp: 1, units: 5);
        session.EndTick(0.1f);
        session.TryEnterCleanup();

        Assert.That(session.State, Is.EqualTo(LevelState.Cleanup));
        Assert.That(session.Boosters.IsRunning(BoosterKind.Turbo), Is.False);
        Assert.That(session.TryActivateBooster(BoosterKind.ExtraTime).IsSuccess, Is.False);
    }

    [Test]
    public void Reset_ClearsEffectsUsageAndEquipmentForTheNextRun()
    {
        var session = CreateStartedSession(LevelType.Normal, BoosterKind.Turbo, BoosterKind.ExtraTime);

        session.TryActivateBooster(BoosterKind.Turbo);
        session.TryActivateBooster(BoosterKind.ExtraTime);
        session.Reset();

        Assert.That(session.IsAssisted, Is.False);
        Assert.That(session.Boosters.IsRunning(BoosterKind.Turbo), Is.False);
        Assert.That(session.Boosters.IsUsed(BoosterKind.Turbo), Is.False);
        Assert.That(session.Boosters.IsUsed(BoosterKind.ExtraTime), Is.False);
        Assert.That(session.Boosters.IsEquipped(BoosterKind.Turbo), Is.False);
        Assert.That(session.RemainingTime, Is.EqualTo(TIME_LIMIT));
        Assert.That(_messages.BoostersEnded, Is.EqualTo(new[] { new BoosterEndedMsg(BoosterKind.Turbo) }));
    }

    [TestCase(LevelType.Tutorial)]
    [TestCase(LevelType.Relax)]
    public void UntimedLevels_AllowNoBoosters(LevelType type)
    {
        var session = CreateStartedSession(type, BoosterKind.Turbo, BoosterKind.ExtraTime);

        Assert.That(session.TryActivateBooster(BoosterKind.Turbo).IsSuccess, Is.False);
        Assert.That(session.TryActivateBooster(BoosterKind.ExtraTime).IsSuccess, Is.False);
        Assert.That(session.IsAssisted, Is.False);
    }

    [Test]
    public void TimerDisabled_HidesExtraTimeButKeepsTurbo()
    {
        var level = _assets.CreateLevel(TIME_LIMIT, new QuotaSettings { Kind = PlantKind.Grass, Amount = 5 });
        var rules = GameRulesValues.Default with { TimerEnabled = false };
        var session = new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher, rules);

        session.TryEquipBooster(BoosterKind.Turbo, isEquipped: true);
        session.TryEquipBooster(BoosterKind.ExtraTime, isEquipped: true);
        session.TryBegin();

        Assert.That(session.TryActivateBooster(BoosterKind.ExtraTime).IsSuccess, Is.False);
        Assert.That(session.TryActivateBooster(BoosterKind.Turbo).IsSuccess, Is.True);
    }

    [Test]
    public void Snapshot_ReportsStateStockAndHudVisibility()
    {
        var session = CreateSession(LevelType.Normal);

        session.TryEquipBooster(BoosterKind.Turbo, isEquipped: true);

        var preview = BoosterSnapshot.From(session, GetStock);

        Assert.That(preview.IsHudShown, Is.False);
        Assert.That(preview.Turbo.State, Is.EqualTo(BoosterSlotState.Ready));
        Assert.That(preview.Turbo.Stock, Is.EqualTo(3));
        Assert.That(preview.ExtraTime.State, Is.EqualTo(BoosterSlotState.NotEquipped));

        session.TryBegin();
        session.TryActivateBooster(BoosterKind.Turbo);

        var playing = BoosterSnapshot.From(session, GetStock);

        Assert.That(playing.IsHudShown, Is.True);
        Assert.That(playing.Turbo.State, Is.EqualTo(BoosterSlotState.Running));
        Assert.That(playing.Turbo.Remaining, Is.EqualTo(8f));
        Assert.That(playing.Turbo.Duration, Is.EqualTo(8f));
        Assert.That(playing.GetSlot(BoosterKind.Turbo), Is.EqualTo(playing.Turbo));
    }

    private static BoosterActivatedMsg Activated(BoosterKind kind, float seconds)
        => new(kind, seconds);

    private static int GetStock(BoosterKind kind)
        => 3;

    private LevelSession CreateSession(LevelType type)
    {
        var level = _assets.CreateLevel(type, TIME_LIMIT, new QuotaSettings { Kind = PlantKind.Grass, Amount = 5 });

        return new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher);
    }

    private LevelSession CreateStartedSession(LevelType type, params BoosterKind[] equipped)
    {
        var session = CreateSession(type);

        for (var i = 0; i < equipped.Length; i++)
        {
            session.TryEquipBooster(equipped[i], isEquipped: true);
        }

        session.TryBegin();
        return session;
    }
}
