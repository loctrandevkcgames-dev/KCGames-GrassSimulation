using System.Buffers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EncosyTower.PubSub;
using EncosyTower.Tasks;
using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrassSimulation.Progression.Tests;

public sealed class LevelSettlementHandlerTests
{
    private const StarFlags GOAL_CLEAN = StarFlags.Goal | StarFlags.Clean;

    private static readonly LevelId s_level = new("level-01");

    private Messenger _messenger;
    private FakeProgressStore _store;
    private ProgressionService _service;
    private LevelSettlementHandler _handler;
    private LevelCatalog _catalog;
    private Object[] _catalogObjects;
    private MessagePublisher.Publisher<GameplayScope> _gameplayPublisher;
    private List<LevelSettledMsg> _settled;
    private List<UnlockGrantedMsg> _unlocks;
    private List<ISubscription> _subscriptions;

    [SetUp]
    public void SetUp()
    {
        _messenger = new Messenger(ArrayPool<UnityTask>.Shared);
        _store = new FakeProgressStore();
        _service = new ProgressionService(_store);
        _service.Initialize();
        _settled = new List<LevelSettledMsg>();
        _unlocks = new List<UnlockGrantedMsg>();
        _subscriptions = new List<ISubscription>();
        _gameplayPublisher = _messenger.Publisher.Scope<GameplayScope>();

        var progressionSubscriber = _messenger.Subscriber.Scope<ProgressionScope>();

        _subscriptions.Add(LevelSettledMsg.Subscribe(in progressionSubscriber, _settled.Add));
        _subscriptions.Add(UnlockGrantedMsg.Subscribe(in progressionSubscriber, _unlocks.Add));

        _catalog = TestCatalogs.Create("level-01", "level-02", "level-03", out _catalogObjects);
        TestCatalogs.SetUnlock(_catalog, index: 0, UnlockKind.WideMachine, amount: 0);

        _handler = new LevelSettlementHandler(
              _service
            , _catalog
            , _messenger.Subscriber.Scope<GameplayScope>()
            , _messenger.Publisher.Scope<ProgressionScope>()
        );
    }

    [TearDown]
    public void TearDown()
    {
        _handler.Dispose();
        _subscriptions.Unsubscribe();
        _messenger.Dispose();
        TestCatalogs.DestroyAll(_catalogObjects);
    }

    [Test]
    public void LevelFinished_SettlesAndPublishesTheStars()
    {
        Finish(GOAL_CLEAN);

        Assert.That(_settled, Has.Count.EqualTo(1));
        Assert.That(_settled[0].Level, Is.EqualTo(s_level));
        Assert.That(_settled[0].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Earned, Is.EqualTo(GOAL_CLEAN));
        Assert.That(settlement.New, Is.EqualTo(GOAL_CLEAN));
        Assert.That(settlement.IsFirstCompletion, Is.True);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(GOAL_CLEAN));
        Assert.That(_handler.HasPending, Is.False);
    }

    [Test]
    public void LevelFinished_WinGrantsTheLevelUnlockAndPublishesIt()
    {
        Finish(StarFlags.Goal);

        Assert.That(_settled[0].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Unlocks, Has.Length.EqualTo(1));
        Assert.That(settlement.Unlocks[0].Kind, Is.EqualTo(UnlockKind.WideMachine));
        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.True);
        Assert.That(_unlocks, Has.Count.EqualTo(1));
        Assert.That(_unlocks[0].Level, Is.EqualTo(s_level));
        Assert.That(_unlocks[0].Unlock.Kind, Is.EqualTo(UnlockKind.WideMachine));
    }

    [Test]
    public void LevelFinishedTwice_GrantsTheUnlockOnce()
    {
        Finish(StarFlags.Goal);
        Finish(GOAL_CLEAN);

        Assert.That(_settled[1].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.Unlocks, Is.Empty);
        Assert.That(_unlocks, Has.Count.EqualTo(1));
    }

    [Test]
    public void LevelFinished_LossGrantsNothing()
    {
        var message = new LevelFinishedMsg(Level: s_level, Result: LevelResults.Loss(), Duration: 30f);

        LevelFinishedMsg.Publish(in _gameplayPublisher, message);

        Assert.That(_unlocks, Is.Empty);
        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.False);
    }

    [Test]
    public void FailingSave_DoesNotKeepTheUnlockAndRetryGrantsIt()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Finish(StarFlags.Goal);

        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.False);
        Assert.That(_unlocks, Is.Empty);

        _store.FailSaves = false;
        _handler.RetryPending();

        Assert.That(_service.IsMachineOwned(MachineIds.Wide), Is.True);
        Assert.That(_unlocks, Has.Count.EqualTo(1));
    }

    [Test]
    public void LevelFinishedTwice_ReportsNoNewStarsTheSecondTime()
    {
        Finish(GOAL_CLEAN);
        Finish(GOAL_CLEAN);

        Assert.That(_settled, Has.Count.EqualTo(2));
        Assert.That(_settled[1].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.New, Is.EqualTo(StarFlags.None));
        Assert.That(settlement.IsFirstCompletion, Is.False);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(GOAL_CLEAN));
    }

    [Test]
    public void FailingSave_KeepsThePendingResultAndRetrySettlesOnce()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Finish(StarRules.ALL);

        Assert.That(_handler.HasPending, Is.True);
        Assert.That(_settled, Has.Count.EqualTo(1));
        Assert.That(_settled[0].Outcome.IsError, Is.True);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));

        _store.FailSaves = false;
        _handler.RetryPending();

        Assert.That(_handler.HasPending, Is.False);
        Assert.That(_settled, Has.Count.EqualTo(2));
        Assert.That(_settled[1].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.New, Is.EqualTo(StarRules.ALL));
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarRules.ALL));

        _handler.RetryPending();

        Assert.That(_settled, Has.Count.EqualTo(2));
        Assert.That(_service.GetAttempts(s_level), Is.EqualTo(1));
    }

    [Test]
    public void StillFailing_RetryKeepsThePending()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));
        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Finish(StarFlags.Goal);
        _handler.RetryPending();

        Assert.That(_handler.HasPending, Is.True);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
    }

    [Test]
    public void SettledSubscriberThatRetries_DoesNotReenterOrLoseAnEntry()
    {
        var subscriber = _messenger.Subscriber.Scope<ProgressionScope>();

        _subscriptions.Add(LevelSettledMsg.Subscribe(in subscriber, (LevelSettledMsg _) => _handler.RetryPending()));

        Finish(GOAL_CLEAN);

        Assert.That(_settled, Has.Count.EqualTo(1));
        Assert.That(_handler.HasPending, Is.False);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(GOAL_CLEAN));
    }

    [Test]
    public void ClearPending_DropsUnsettledResults()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Finish(StarRules.ALL);

        Assert.That(_handler.HasPending, Is.True);

        _handler.ClearPending();
        _store.FailSaves = false;
        _handler.RetryPending();

        Assert.That(_handler.HasPending, Is.False);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
    }

    [Test]
    public void Dispose_StopsListening()
    {
        _handler.Dispose();

        Finish(StarRules.ALL);

        Assert.That(_settled, Is.Empty);
        Assert.That(_service.GetStars(s_level), Is.EqualTo(StarFlags.None));
    }

    private void Finish(StarFlags stars)
    {
        var message = new LevelFinishedMsg(Level: s_level, Result: LevelResults.Win(stars), Duration: 30f);

        LevelFinishedMsg.Publish(in _gameplayPublisher, message);
    }
}
