using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class MachineTests
{
    private const float WIDE_RADIUS = 0.78f;
    private const float WIDE_SPEED = 3.6f;
    private const int CUTTABLE_CELLS = 100;

    private TestAssets _assets;
    private MessageRecorder _messages;
    private MachineConfig _standard;
    private MachineConfig _wide;
    private MachineCatalog _catalog;
    private LevelDefinition _level;

    [SetUp]
    public void SetUp()
    {
        _assets = new TestAssets();
        _messages = new MessageRecorder();
        _standard = _assets.CreateMachine("standard", cutRadius: 0.65f, speed: 4f);
        _wide = _assets.CreateMachine("wide", WIDE_RADIUS, WIDE_SPEED);
        _catalog = _assets.CreateMachineCatalog(_standard, _wide);
        _level = _assets.CreateLevel(timeLimit: 10f, new QuotaSettings { Kind = PlantKind.Grass, Amount = 50 });
    }

    [TearDown]
    public void TearDown()
    {
        _messages.Dispose();
        _assets.Dispose();
    }

    [Test]
    public void Catalog_FindsAMachineById()
    {
        Assert.That(_catalog.TryFind(MachineIds.Wide, out var machine), Is.True);
        Assert.That(machine, Is.SameAs(_wide));
        Assert.That(_catalog.TryFind(MachineIds.Standard, out machine), Is.True);
        Assert.That(machine, Is.SameAs(_standard));
    }

    [Test]
    public void Catalog_MissesAnUnknownId()
    {
        Assert.That(_catalog.TryFind(new MachineId("heavy"), out var machine), Is.False);
        Assert.That(machine, Is.Null);
        Assert.That(_catalog.Count, Is.EqualTo(2));
        Assert.That(_catalog.Standard, Is.SameAs(_standard));
    }

    [Test]
    public void WideSession_UsesTheWideBaseStats()
    {
        var session = CreateSession(_wide);
        var stats = session.Growth.Stats;

        Assert.That(stats.CutRadius, Is.EqualTo(WIDE_RADIUS).Within(1e-4f));
        Assert.That(stats.Speed, Is.EqualTo(WIDE_SPEED).Within(1e-4f));
        Assert.That(stats.CuttingPower, Is.EqualTo(1f).Within(1e-4f));
    }

    [Test]
    public void WideWithThreeWideBlades_ReachesTheMaximumRadius()
    {
        var session = CreateSession(_wide);

        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 500);

        for (var i = 0; i < 3; i++)
        {
            session.EndTick(0.01f);
            Assert.That(session.TryChooseUpgrade(option: 0), Is.True);
        }

        Assert.That(session.Growth.Stats.CutRadius, Is.EqualTo(1.23f).Within(1e-4f));
    }

    [Test]
    public void TryBegin_WithAnotherMachine_SwitchesTheStatsAndAnnouncesIt()
    {
        var session = CreateSession(_standard);

        Assert.That(session.TryBegin(_wide), Is.True);

        Assert.That(session.Machine, Is.SameAs(_wide));
        Assert.That(session.Growth.Stats.CutRadius, Is.EqualTo(WIDE_RADIUS).Within(1e-4f));
        Assert.That(_messages.LevelStarted, Has.Count.EqualTo(1));
        Assert.That(_messages.LevelStarted[0].Machine, Is.EqualTo(MachineIds.Wide));
        Assert.That(_messages.LevelStarted[0].Level, Is.EqualTo(_level.Id));
    }

    [Test]
    public void Reset_KeepsTheChosenMachine()
    {
        var session = CreateSession(_standard);

        session.TryBegin(_wide);
        session.Reset();

        Assert.That(session.State, Is.EqualTo(LevelState.Preview));
        Assert.That(session.Machine, Is.SameAs(_wide));
        Assert.That(session.Growth.Stats.CutRadius, Is.EqualTo(WIDE_RADIUS).Within(1e-4f));
        Assert.That(session.Growth.Tier, Is.EqualTo(1));

        session.TryBegin();

        Assert.That(_messages.LevelStarted[^1].Machine, Is.EqualTo(MachineIds.Wide));
    }

    [Test]
    public void UsingAWideMachine_DoesNotMakeTheRunAssisted()
    {
        var session = CreateSession(_standard);

        session.TryBegin(_wide);

        Assert.That(session.IsAssisted, Is.False);
    }

    [Test]
    public void Preview_OpensTheLoadoutAndGoesBack()
    {
        var session = CreateSession(_standard);

        Assert.That(session.TryOpenLoadout(), Is.True);
        Assert.That(session.State, Is.EqualTo(LevelState.Loadout));
        Assert.That(session.BackToPreview(), Is.True);
        Assert.That(session.State, Is.EqualTo(LevelState.Preview));
        Assert.That(
              _messages.StateChanges.ConvertAll(message => message.State)
            , Is.EqualTo(new[] { LevelState.Loadout, LevelState.Preview })
        );
    }

    [Test]
    public void Loadout_BeginsThePlayingState()
    {
        var session = CreateSession(_standard);

        session.TryOpenLoadout();

        Assert.That(session.TryBegin(_wide), Is.True);
        Assert.That(session.State, Is.EqualTo(LevelState.Playing));
    }

    [Test]
    public void Loadout_DoesNotRunTheTimerOrAcceptHarvests()
    {
        var session = CreateSession(_standard);

        session.TryOpenLoadout();
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(1f);
        session.Pause();

        Assert.That(session.IsSimulating, Is.False);
        Assert.That(session.IsPaused, Is.False);
        Assert.That(session.RemainingTime, Is.EqualTo(10f));
        Assert.That(session.Objectives.GetHarvested(PlantKind.Grass), Is.Zero);
    }

    [Test]
    public void LoadoutAndBack_AreOnlyValidFromTheirOwnStates()
    {
        var session = CreateSession(_standard);

        Assert.That(session.BackToPreview(), Is.False);

        session.TryBegin();

        Assert.That(session.TryOpenLoadout(), Is.False);
        Assert.That(session.TryBegin(_wide), Is.False);
        Assert.That(session.Machine, Is.SameAs(_standard));
    }

    [Test]
    public void LoadoutRules_SkipTheLoadoutForOnlyTheStandardMachineWithoutBoosters()
    {
        Assert.That(LoadoutRules.ShouldShow(ownedMachineCount: 1, hasBoosterStock: false), Is.False);
        Assert.That(LoadoutRules.ShouldShow(ownedMachineCount: 2, hasBoosterStock: false), Is.True);
        Assert.That(LoadoutRules.ShouldShow(ownedMachineCount: 1, hasBoosterStock: true), Is.True);
    }

    [Test]
    public void LoadoutSnapshot_ListsOnlyOwnedMachinesWithTheirBaseStats()
    {
        var onlyStandard = LoadoutSnapshot.From(_catalog, id => id == MachineIds.Standard, MachineIds.Standard);

        Assert.That(onlyStandard.MachineCount, Is.EqualTo(1));
        Assert.That(onlyStandard.Machine0.Id, Is.EqualTo(MachineIds.Standard));

        var both = LoadoutSnapshot.From(_catalog, _ => true, MachineIds.Wide);

        Assert.That(both.MachineCount, Is.EqualTo(2));
        Assert.That(both.Selected, Is.EqualTo(MachineIds.Wide));
        Assert.That(both.GetMachine(1).Id, Is.EqualTo(MachineIds.Wide));
        Assert.That(both.GetMachine(1).Stats.CutRadius, Is.EqualTo(WIDE_RADIUS).Within(1e-4f));
        Assert.That(both.GetMachine(1).Stats.Speed, Is.EqualTo(WIDE_SPEED).Within(1e-4f));
    }

    [Test]
    public void MachineIds_MapOnlyTheWideUnlockToAMachine()
    {
        Assert.That(MachineIds.TryFromUnlock(UnlockKind.WideMachine, out var machine), Is.True);
        Assert.That(machine, Is.EqualTo(MachineIds.Wide));
        Assert.That(MachineIds.TryFromUnlock(UnlockKind.TurboBooster, out _), Is.False);
        Assert.That(MachineIds.TryFromUnlock(UnlockKind.None, out _), Is.False);
    }

    private LevelSession CreateSession(MachineConfig machine)
        => new(_level, machine, CUTTABLE_CELLS, _messages.Publisher);
}
