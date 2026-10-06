using System.Collections.Generic;
using EncosyTower.Processing;
using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelSnapshotTests
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
    public void From_ReportsLevelStateTimeAndQuotas()
    {
        var level = _assets.CreateLevel(
              timeLimit: 30f
            , failOnProtectedHits: true
            , Quota(PlantKind.Grass, 4)
            , Quota(PlantKind.HarvestFlower, 2, isBonus: true)
        );

        var session = CreateSession(level);
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 10);
        session.RecordHarvest(PlantKind.Grass, xp: 10);
        session.EndTick(1.5f);

        var snapshot = LevelSnapshot.From(session, level, levelIndex: 2, levelCount: 7);

        Assert.That(snapshot.Level, Is.EqualTo(new LevelId("level-00")));
        Assert.That(snapshot.LevelIndex, Is.EqualTo(2));
        Assert.That(snapshot.LevelCount, Is.EqualTo(7));
        Assert.That(snapshot.State, Is.EqualTo(LevelState.Playing));
        Assert.That(snapshot.IsPaused, Is.False);
        Assert.That(snapshot.RemainingTime, Is.EqualTo(28.5f).Within(1e-4f));
        Assert.That(snapshot.TimeLimit, Is.EqualTo(30f));
        Assert.That(snapshot.ClearedFraction, Is.EqualTo(0.02f).Within(1e-5f));
        Assert.That(snapshot.CountsProtectedHits, Is.True);
        Assert.That(snapshot.ProtectedHitLimit, Is.EqualTo(3));
        Assert.That(snapshot.ProtectedHits, Is.Zero);
        Assert.That(snapshot.QuotaCount, Is.EqualTo(2));
        var grass = new QuotaSnapshot(Kind: PlantKind.Grass, Amount: 4, Progress: 2, IsBonus: false, IsMet: false);
        var flowers = new QuotaSnapshot(
              Kind: PlantKind.HarvestFlower
            , Amount: 2
            , Progress: 0
            , IsBonus: true
            , IsMet: false
        );

        Assert.That(snapshot.GetQuota(0), Is.EqualTo(grass));
        Assert.That(snapshot.GetQuota(1), Is.EqualTo(flowers));
        Assert.That(snapshot.GetQuota(2), Is.EqualTo(default(QuotaSnapshot)));
    }

    [Test]
    public void From_MarksMetQuotas()
    {
        var level = _assets.CreateLevel(
              timeLimit: 10f
            , failOnProtectedHits: false
            , Quota(PlantKind.Grass, 2)
            , Quota(PlantKind.HarvestFlower, 0, isBonus: true)
        );

        var session = CreateSession(level);
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 0);
        session.RecordHarvest(PlantKind.Grass, xp: 0);
        session.RecordHarvest(PlantKind.Grass, xp: 0);

        var snapshot = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1);

        Assert.That(snapshot.GetQuota(0).IsMet, Is.True);
        Assert.That(snapshot.GetQuota(0).Progress, Is.EqualTo(2));
        Assert.That(snapshot.GetQuota(1).IsMet, Is.True);
    }

    [Test]
    public void From_ReportsTierFloorAndNextThreshold()
    {
        var level = _assets.CreateLevel(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 99));
        var session = CreateSession(level);
        session.TryBegin();

        var tierOne = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1);

        Assert.That(tierOne.Tier, Is.EqualTo(1));
        Assert.That(tierOne.TierFloorXp, Is.Zero);
        Assert.That(tierOne.NextThresholdXp.TryGetValue(out var tierOneThreshold), Is.True);
        Assert.That(tierOneThreshold, Is.EqualTo(100));

        session.RecordHarvest(PlantKind.Grass, xp: 130);

        var tierTwo = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1);

        Assert.That(tierTwo.Tier, Is.EqualTo(2));
        Assert.That(tierTwo.Xp, Is.EqualTo(130));
        Assert.That(tierTwo.TierFloorXp, Is.EqualTo(100));
        Assert.That(tierTwo.NextThresholdXp.TryGetValue(out var tierTwoThreshold), Is.True);
        Assert.That(tierTwoThreshold, Is.EqualTo(260));
        Assert.That(tierTwo.PendingUpgrades, Is.EqualTo(1));

        session.RecordHarvest(PlantKind.Grass, xp: 400);

        var maxTier = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1);

        Assert.That(maxTier.Tier, Is.EqualTo(4));
        Assert.That(maxTier.TierFloorXp, Is.EqualTo(480));
        Assert.That(maxTier.NextThresholdXp.HasValue, Is.False);
    }

    [Test]
    public void From_ReportsUpgradeBeforeAndAfterStats()
    {
        var level = _assets.CreateLevel(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 99));
        var session = CreateSession(level);
        session.TryBegin();

        var snapshot = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1);

        Assert.That(snapshot.Stats.CutRadius, Is.EqualTo(0.65f).Within(1e-5f));
        Assert.That(snapshot.UpgradeOptionCount, Is.EqualTo(2));
        Assert.That(snapshot.GetUpgradeId(0), Is.EqualTo("WideBlade"));
        Assert.That(snapshot.GetUpgradeId(1), Is.EqualTo("StrongEngine"));
        Assert.That(snapshot.Upgrade0Stats.CutRadius, Is.EqualTo(0.80f).Within(1e-5f));
        Assert.That(snapshot.Upgrade0Stats.CuttingPower, Is.EqualTo(snapshot.Stats.CuttingPower));
        Assert.That(snapshot.Upgrade1Stats.CuttingPower, Is.EqualTo(1.3f).Within(1e-5f));
        Assert.That(snapshot.Upgrade1Stats.Speed, Is.EqualTo(4.25f).Within(1e-5f));
        Assert.That(snapshot.Upgrade1Stats.CutRadius, Is.EqualTo(snapshot.Stats.CutRadius));
        Assert.That(snapshot.Stats, Is.EqualTo(session.Growth.Stats));
    }

    [Test]
    public void From_UpgradeStatsRespectTheMaxCaps()
    {
        var level = _assets.CreateLevel(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 99));
        var session = CreateSession(level);
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 480);
        session.EndTick(0.01f);
        session.TryChooseUpgrade(0);
        session.TryChooseUpgrade(0);
        session.TryChooseUpgrade(0);

        var snapshot = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1);

        Assert.That(snapshot.Stats.CutRadius, Is.EqualTo(1.1f).Within(1e-5f));
        Assert.That(snapshot.Upgrade0Stats.CutRadius, Is.EqualTo(1.1f).Within(1e-5f));
    }

    [Test]
    public void From_ReportsTheTierOfTheUpgradeBeingChosenAndItsUnlocks()
    {
        var level = _assets.CreateLevel(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 99));
        var session = CreateSession(level);
        session.TryBegin();
        var plants = new[] {
            new PlantSettings { Kind = PlantKind.Grass, RequiredTier = 1 },
            new PlantSettings { Kind = PlantKind.LowBush, RequiredTier = 2 },
            new PlantSettings { Kind = PlantKind.HardBush, RequiredTier = 3 },
        };

        var unlocked = PlantKindMask.FromTier(plants, session.Growth.UpgradeTier);
        var tierOne = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1, unlockedKinds: unlocked);

        Assert.That(tierOne.UpgradeTier, Is.EqualTo(1));
        Assert.That(tierOne.UnlockedKinds.Count, Is.EqualTo(1));

        session.RecordHarvest(PlantKind.Grass, xp: 260);
        session.EndTick(0.01f);

        var unlocked2 = PlantKindMask.FromTier(plants, session.Growth.UpgradeTier);
        var twoPending = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1, unlockedKinds: unlocked2);

        Assert.That(twoPending.Tier, Is.EqualTo(3));
        Assert.That(twoPending.PendingUpgrades, Is.EqualTo(2));
        Assert.That(twoPending.UpgradeTier, Is.EqualTo(2));
        Assert.That(twoPending.UnlockedKinds.Contains(PlantKind.LowBush), Is.True);
        Assert.That(twoPending.UnlockedKinds.Contains(PlantKind.HardBush), Is.False);

        session.TryChooseUpgrade(0);

        var unlocked3 = PlantKindMask.FromTier(plants, session.Growth.UpgradeTier);
        var onePending = LevelSnapshot.From(session, level, levelIndex: 0, levelCount: 1, unlockedKinds: unlocked3);

        Assert.That(onePending.UpgradeTier, Is.EqualTo(3));
        Assert.That(onePending.UnlockedKinds.Contains(PlantKind.HardBush), Is.True);
        Assert.That(onePending.UnlockedKinds.Contains(PlantKind.LowBush), Is.False);
    }

    [Test]
    public void Request_ReturnsTheSnapshotThroughTheScopedHub()
    {
        var level = _assets.CreateLevel(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));
        var session = CreateSession(level);
        var registries = new List<ProcessRegistry>();

        using var processor = new Processor();

        var registration = processor.Scope<GameplayScope>().WithRegistries(registries);

        GetLevelSnapshotRequest.Register(
              in registration
            , (GetLevelSnapshotRequest _) => LevelSnapshot.From(session, level, levelIndex: 1, levelCount: 3)
        );

        var hub = processor.Scope<GameplayScope>();
        var result = GetLevelSnapshotRequest.TryProcess(in hub, new GetLevelSnapshotRequest());

        Assert.That(result.TryGetValue(out var snapshot), Is.True);
        Assert.That(snapshot.LevelIndex, Is.EqualTo(1));
        Assert.That(snapshot.State, Is.EqualTo(LevelState.Preview));

        registries.Unregister();

        var afterUnregister = GetLevelSnapshotRequest.TryProcess(
              in hub
            , new GetLevelSnapshotRequest()
            , ProcessingContext.DropIfNoHandler(warnNoHandler: false)
        );

        Assert.That(afterUnregister.HasValue, Is.False);
    }

    private static QuotaSettings Quota(PlantKind kind, int amount, bool isBonus = false)
        => new() { Kind = kind, Amount = amount, IsBonus = isBonus };

    private LevelSession CreateSession(LevelDefinition level)
        => new(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher);
}
