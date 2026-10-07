using System.Buffers;
using System.Text.RegularExpressions;
using EncosyTower.PubSub;
using EncosyTower.Tasks;
using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrassSimulation.Progression.Tests;

public sealed class BoosterConsumptionHandlerTests
{
    private Messenger _messenger;
    private FakeProgressStore _store;
    private ProgressionService _service;
    private BoosterConsumptionHandler _handler;
    private MessagePublisher.Publisher<GameplayScope> _publisher;

    [SetUp]
    public void SetUp()
    {
        _messenger = new Messenger(ArrayPool<UnityTask>.Shared);
        _store = new FakeProgressStore();
        _service = new ProgressionService(_store);
        _service.Initialize();
        _publisher = _messenger.Publisher.Scope<GameplayScope>();

        var unlock = new UnlockSettings { Kind = UnlockKind.TurboBooster };

        _service.Settle(new LevelId("level-06"), LevelResults.Win(StarFlags.Goal), unlock);
        _handler = new BoosterConsumptionHandler(_service, _messenger.Subscriber.Scope<GameplayScope>());
    }

    [TearDown]
    public void TearDown()
    {
        _handler.Dispose();
        _messenger.Dispose();
    }

    [Test]
    public void BoosterActivated_SpendsOneFromTheStockAndSaves()
    {
        var saves = _store.SaveCount;

        BoosterActivatedMsg.Publish(in _publisher, new BoosterActivatedMsg(BoosterKind.Turbo, Seconds: 8f));

        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(2));
        Assert.That(_store.SaveCount, Is.EqualTo(saves + 1));
    }

    [Test]
    public void BoosterActivated_WithoutStock_LogsAnErrorAndChangesNothing()
    {
        LogAssert.Expect(LogType.Error, new Regex("was activated but its stock was not saved"));

        BoosterActivatedMsg.Publish(in _publisher, new BoosterActivatedMsg(BoosterKind.ExtraTime, Seconds: 15f));

        Assert.That(_service.GetBoosterStock(BoosterKind.ExtraTime), Is.Zero);
    }

    [Test]
    public void Dispose_StopsConsuming()
    {
        _handler.Dispose();

        BoosterActivatedMsg.Publish(in _publisher, new BoosterActivatedMsg(BoosterKind.Turbo, Seconds: 8f));

        Assert.That(_service.GetBoosterStock(BoosterKind.Turbo), Is.EqualTo(3));
    }
}
