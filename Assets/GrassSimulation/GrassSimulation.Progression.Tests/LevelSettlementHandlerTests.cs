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
    private static readonly LevelId s_level = new("level-01");

    private Messenger _messenger;
    private FakeProgressStore _store;
    private ProgressionService _service;
    private LevelSettlementHandler _handler;
    private MessagePublisher.Publisher<GameplayScope> _gameplayPublisher;
    private List<LevelSettledMsg> _settled;
    private List<ISubscription> _subscriptions;

    [SetUp]
    public void SetUp()
    {
        _messenger = new Messenger(ArrayPool<UnityTask>.Shared);
        _store = new FakeProgressStore();
        _service = new ProgressionService(_store);
        _service.Initialize();
        _settled = new List<LevelSettledMsg>();
        _subscriptions = new List<ISubscription>();
        _gameplayPublisher = _messenger.Publisher.Scope<GameplayScope>();

        var progressionSubscriber = _messenger.Subscriber.Scope<ProgressionScope>();

        _subscriptions.Add(LevelSettledMsg.Subscribe(in progressionSubscriber, _settled.Add));

        _handler = new LevelSettlementHandler(
              _service
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
    }

    [Test]
    public void LevelFinished_SettlesAndPublishesTheCoins()
    {
        Finish(stars: 2);

        Assert.That(_settled, Has.Count.EqualTo(1));
        Assert.That(_settled[0].Level, Is.EqualTo(s_level));
        Assert.That(_settled[0].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.CoinsGranted, Is.EqualTo(150));
        Assert.That(_service.Coins, Is.EqualTo(150));
        Assert.That(_handler.HasPending, Is.False);
    }

    [Test]
    public void LevelFinishedTwice_PaysOnce()
    {
        Finish(stars: 2);
        Finish(stars: 2);

        Assert.That(_settled, Has.Count.EqualTo(2));
        Assert.That(_settled[1].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.CoinsGranted, Is.Zero);
        Assert.That(_service.Coins, Is.EqualTo(150));
    }

    [Test]
    public void FailingSave_KeepsThePendingResultAndRetrySettlesOnce()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Finish(stars: 3);

        Assert.That(_handler.HasPending, Is.True);
        Assert.That(_settled, Has.Count.EqualTo(1));
        Assert.That(_settled[0].Outcome.IsError, Is.True);
        Assert.That(_service.Coins, Is.Zero);

        _store.FailSaves = false;
        _handler.RetryPending();

        Assert.That(_handler.HasPending, Is.False);
        Assert.That(_settled, Has.Count.EqualTo(2));
        Assert.That(_settled[1].Outcome.TryGetValue(out var settlement), Is.True);
        Assert.That(settlement.CoinsGranted, Is.EqualTo(175));
        Assert.That(_service.Coins, Is.EqualTo(175));

        _handler.RetryPending();

        Assert.That(_settled, Has.Count.EqualTo(2));
        Assert.That(_service.Coins, Is.EqualTo(175));
    }

    [Test]
    public void StillFailing_RetryKeepsThePending()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));
        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Finish(stars: 1);
        _handler.RetryPending();

        Assert.That(_handler.HasPending, Is.True);
        Assert.That(_service.Coins, Is.Zero);
    }

    [Test]
    public void SettledSubscriberThatRetries_DoesNotReenterOrLoseAnEntry()
    {
        var subscriber = _messenger.Subscriber.Scope<ProgressionScope>();

        _subscriptions.Add(LevelSettledMsg.Subscribe(in subscriber, (LevelSettledMsg _) => _handler.RetryPending()));

        Finish(stars: 2);

        Assert.That(_settled, Has.Count.EqualTo(1));
        Assert.That(_handler.HasPending, Is.False);
        Assert.That(_service.Coins, Is.EqualTo(150));
    }

    [Test]
    public void ClearPending_DropsUnsettledResults()
    {
        _store.FailSaves = true;

        LogAssert.Expect(LogType.Error, new Regex("rolled back"));

        Finish(stars: 3);

        Assert.That(_handler.HasPending, Is.True);

        _handler.ClearPending();
        _store.FailSaves = false;
        _handler.RetryPending();

        Assert.That(_handler.HasPending, Is.False);
        Assert.That(_service.Coins, Is.Zero);
    }

    [Test]
    public void Dispose_StopsListening()
    {
        _handler.Dispose();

        Finish(stars: 3);

        Assert.That(_settled, Is.Empty);
        Assert.That(_service.Coins, Is.Zero);
    }

    private void Finish(int stars)
    {
        var message = new LevelFinishedMsg(Level: s_level, Result: LevelResults.Win(stars), Duration: 30f);

        LevelFinishedMsg.Publish(in _gameplayPublisher, message);
    }
}
