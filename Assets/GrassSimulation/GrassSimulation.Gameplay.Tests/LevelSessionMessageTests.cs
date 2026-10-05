using System.Collections.Generic;
using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelSessionMessageTests
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
    public void Begin_PublishesLevelStartedOnce()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));

        session.TryBegin();
        session.TryBegin();

        Assert.That(_messages.LevelStarted, Has.Count.EqualTo(1));
        Assert.That(_messages.LevelStarted[0].Level, Is.EqualTo(new LevelId("level-00")));
    }

    [Test]
    public void Finish_PublishesLevelFinishedOnceWithOutcomeAndDuration()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 1));
        session.TryBegin();

        session.EndTick(0.5f);
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(0.5f);
        session.EndTick(0.5f);

        Assert.That(_messages.LevelFinished, Has.Count.EqualTo(1));

        var message = _messages.LevelFinished[0];

        Assert.That(message.Result, Is.EqualTo(session.Result));
        Assert.That(message.Result.TryGetStars(out var stars), Is.True);
        Assert.That(stars, Is.GreaterThan(0));
        Assert.That(message.Duration, Is.EqualTo(1f).Within(1e-4f));
    }

    [Test]
    public void Failure_PublishesLevelFinishedWithFailureOutcome()
    {
        var session = CreateSession(timeLimit: 1f, failOnProtectedHits: false, Quota(PlantKind.Grass, 5));
        session.TryBegin();

        session.EndTick(2f);

        Assert.That(_messages.LevelFinished, Has.Count.EqualTo(1));
        Assert.That(_messages.LevelFinished[0].Result.TryGetStars(out _), Is.False);
        Assert.That(_messages.LevelFinished[0].Result.IsFinished, Is.True);
    }

    [Test]
    public void Xp_PublishesTierUpWhenThresholdCrossed()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        session.TryBegin();

        session.RecordHarvest(PlantKind.Grass, xp: 99);
        Assert.That(_messages.TierUps, Is.Empty);

        session.RecordXp(1);

        Assert.That(_messages.TierUps, Has.Count.EqualTo(1));
        Assert.That(_messages.TierUps[0].Tier, Is.EqualTo(2));
    }

    [Test]
    public void Xp_PublishesOneTierUpPerTierGained()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        session.TryBegin();

        session.RecordHarvest(PlantKind.Grass, xp: 300);

        Assert.That(_messages.TierUps, Has.Count.EqualTo(2));
        Assert.That(_messages.TierUps[0].Tier, Is.EqualTo(2));
        Assert.That(_messages.TierUps[1].Tier, Is.EqualTo(3));
    }

    [Test]
    public void Quota_PublishesCompletedExactlyOnceEvenWithExtraHarvests()
    {
        var session = CreateSession(
              timeLimit: 10f
            , failOnProtectedHits: false
            , Quota(PlantKind.Grass, 2)
            , Quota(PlantKind.HarvestFlower, 1, isBonus: true)
        );

        session.TryBegin();

        session.RecordHarvest(PlantKind.Grass, xp: 0);
        Assert.That(_messages.QuotasCompleted, Is.Empty);

        session.RecordHarvest(PlantKind.Grass, xp: 0);
        session.RecordHarvest(PlantKind.Grass, xp: 0);
        session.RecordHarvest(PlantKind.Grass, xp: 0);
        session.RecordHarvest(PlantKind.HarvestFlower, xp: 0);
        session.RecordHarvest(PlantKind.HarvestFlower, xp: 0);

        Assert.That(_messages.QuotasCompleted, Has.Count.EqualTo(2));
        Assert.That(_messages.QuotasCompleted[0], Is.EqualTo(new QuotaCompletedMsg(0, PlantKind.Grass, false)));
        Assert.That(_messages.QuotasCompleted[1], Is.EqualTo(new QuotaCompletedMsg(1, PlantKind.HarvestFlower, true)));
    }

    [Test]
    public void ProtectedTouch_PublishesOnlyOnNewHits()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: true, Quota(PlantKind.Grass, 5));
        session.TryBegin();

        session.RecordProtectedTouch();
        session.EndTick(0.2f);
        session.RecordProtectedTouch();
        session.EndTick(1.5f);
        session.RecordProtectedTouch();

        Assert.That(_messages.ProtectedHits, Has.Count.EqualTo(2));
        Assert.That(_messages.ProtectedHits[0], Is.EqualTo(new ProtectedHitMsg(1, 3, true)));
        Assert.That(_messages.ProtectedHits[1], Is.EqualTo(new ProtectedHitMsg(2, 3, true)));
    }

    [Test]
    public void Harvests_AreBatchedWithinTheInterval()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        session.TryBegin();

        session.RecordHarvest(PlantKind.Grass, xp: 2);
        session.RecordHarvest(PlantKind.Grass, xp: 3);
        session.EndTick(0.04f);
        session.RecordHarvest(PlantKind.Grass, xp: 5);
        session.EndTick(0.04f);

        Assert.That(_messages.HarvestBatches, Is.Empty);

        session.EndTick(0.04f);

        Assert.That(_messages.HarvestBatches, Has.Count.EqualTo(1));
        Assert.That(_messages.HarvestBatches[0], Is.EqualTo(new HarvestBatchedMsg(3, 10)));

        session.EndTick(0.5f);

        Assert.That(_messages.HarvestBatches, Has.Count.EqualTo(1));
    }

    [Test]
    public void Finish_FlushesTheRemainingHarvestBatchBeforeLevelFinished()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 2));
        session.TryBegin();

        session.RecordHarvest(PlantKind.Grass, xp: 4);
        session.RecordHarvest(PlantKind.Grass, xp: 6);
        session.EndTick(0.01f);

        Assert.That(_messages.HarvestBatches, Has.Count.EqualTo(1));
        Assert.That(_messages.HarvestBatches[0], Is.EqualTo(new HarvestBatchedMsg(2, 10)));
        Assert.That(_messages.LevelFinished, Has.Count.EqualTo(1));
    }

    [Test]
    public void Cleanup_BatchesHarvestedCells()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 1));
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(0.01f);
        session.TryEnterCleanup();
        _messages.HarvestBatches.Clear();

        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.EndTick(0.2f);

        Assert.That(_messages.HarvestBatches, Has.Count.EqualTo(1));
        Assert.That(_messages.HarvestBatches[0], Is.EqualTo(new HarvestBatchedMsg(2, 0)));
    }

    [Test]
    public void Reset_ClearsPendingHarvestBatch()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 1);

        session.Reset();
        session.TryBegin();
        session.EndTick(1f);

        Assert.That(_messages.HarvestBatches, Is.Empty);
    }

    [Test]
    public void ChoosingUpgrade_PublishesUpgradeChosen()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 100);
        session.EndTick(0.01f);

        session.TryChooseUpgrade(1);
        session.TryChooseUpgrade(1);

        Assert.That(_messages.UpgradesChosen, Has.Count.EqualTo(1));
        Assert.That(_messages.UpgradesChosen[0], Is.EqualTo(new UpgradeChosenMsg(1, "StrongEngine", 2)));
    }

    [Test]
    public void ChoosingUpgrades_AfterMultipleTiersGained_ResolvesEachTierInOrder()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        var statesSeen = new List<LevelState>();
        var subscriber = _messages.Subscriber;

        UpgradeChosenMsg.Subscribe(in subscriber, (UpgradeChosenMsg _) => statesSeen.Add(session.State));
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 300);
        session.EndTick(0.01f);

        Assert.That(_messages.TierUps, Has.Count.EqualTo(2));

        session.TryChooseUpgrade(0);
        session.TryChooseUpgrade(1);

        Assert.That(_messages.UpgradesChosen, Has.Count.EqualTo(2));
        Assert.That(_messages.UpgradesChosen[0].Tier, Is.EqualTo(2));
        Assert.That(_messages.UpgradesChosen[1].Tier, Is.EqualTo(3));
        Assert.That(statesSeen, Is.EqualTo(new[] { LevelState.UpgradeChoice, LevelState.Playing }));
    }

    [Test]
    public void Begin_PublishesQuotasThatAreMetFromTheStart()
    {
        var session = CreateSession(
              timeLimit: 10f
            , failOnProtectedHits: false
            , Quota(PlantKind.Grass, 3)
            , Quota(PlantKind.HarvestFlower, 0, isBonus: true)
        );

        session.TryBegin();

        Assert.That(_messages.QuotasCompleted, Has.Count.EqualTo(1));
        Assert.That(_messages.QuotasCompleted[0], Is.EqualTo(new QuotaCompletedMsg(1, PlantKind.HarvestFlower, true)));

        session.RecordHarvest(PlantKind.HarvestFlower, xp: 0);

        Assert.That(_messages.QuotasCompleted, Has.Count.EqualTo(1));
    }

    [Test]
    public void RecordXp_InsideTheWindow_BatchesXpWithoutCells()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        session.TryBegin();

        session.RecordXp(7);
        session.EndTick(0.2f);

        Assert.That(_messages.HarvestBatches, Has.Count.EqualTo(1));
        Assert.That(_messages.HarvestBatches[0], Is.EqualTo(new HarvestBatchedMsg(0, 7)));
    }

    [Test]
    public void PausedOrUpgradeChoice_DefersTheBatchUntilPlayResumes()
    {
        var session = CreateSession(timeLimit: 10f, failOnProtectedHits: false, Quota(PlantKind.Grass, 50));
        session.TryBegin();
        session.RecordHarvest(PlantKind.Grass, xp: 1);
        session.Pause();

        session.EndTick(0.5f);

        Assert.That(_messages.HarvestBatches, Is.Empty);

        session.Resume();
        session.RecordHarvest(PlantKind.Grass, xp: 100);
        session.EndTick(0.01f);

        Assert.That(session.State, Is.EqualTo(LevelState.UpgradeChoice));
        session.EndTick(0.5f);

        Assert.That(_messages.HarvestBatches, Is.Empty);

        session.TryChooseUpgrade(0);
        session.EndTick(0.2f);

        Assert.That(_messages.HarvestBatches, Has.Count.EqualTo(1));
        Assert.That(_messages.HarvestBatches[0], Is.EqualTo(new HarvestBatchedMsg(2, 101)));
    }

    private static QuotaSettings Quota(PlantKind kind, int amount, bool isBonus = false)
        => new() { Kind = kind, Amount = amount, IsBonus = isBonus };

    private LevelSession CreateSession(float timeLimit, bool failOnProtectedHits, params QuotaSettings[] quotas)
    {
        var level = _assets.CreateLevel(timeLimit, failOnProtectedHits, quotas);
        return new LevelSession(level, _assets.CreateMachine(), CUTTABLE_CELLS, _messages.Publisher);
    }
}
