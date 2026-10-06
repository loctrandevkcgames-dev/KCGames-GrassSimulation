using System;
using System.Buffers;
using System.Collections.Generic;
using EncosyTower.PubSub;
using EncosyTower.Tasks;

namespace GrassSimulation.Gameplay.Tests;

internal sealed class MessageRecorder : IDisposable
{
    private readonly Messenger _messenger = new(ArrayPool<UnityTask>.Shared);
    private readonly List<ISubscription> _subscriptions = new();

    public MessageRecorder()
    {
        Publisher = _messenger.Publisher.Scope<GameplayScope>();
        Subscriber = _messenger.Subscriber.Scope<GameplayScope>();

        var subscriber = Subscriber;

        _subscriptions.Add(LevelStartedMsg.Subscribe(in subscriber, LevelStarted.Add));
        _subscriptions.Add(LevelFinishedMsg.Subscribe(in subscriber, LevelFinished.Add));
        _subscriptions.Add(TierUpMsg.Subscribe(in subscriber, TierUps.Add));
        _subscriptions.Add(UpgradeChosenMsg.Subscribe(in subscriber, UpgradesChosen.Add));
        _subscriptions.Add(QuotaCompletedMsg.Subscribe(in subscriber, QuotasCompleted.Add));
        _subscriptions.Add(ProtectedHitMsg.Subscribe(in subscriber, ProtectedHits.Add));
        _subscriptions.Add(HarvestBatchedMsg.Subscribe(in subscriber, HarvestBatches.Add));
        _subscriptions.Add(LevelStateChangedMsg.Subscribe(in subscriber, StateChanges.Add));
    }

    public MessagePublisher.Publisher<GameplayScope> Publisher { get; }

    public MessageSubscriber.Subscriber<GameplayScope> Subscriber { get; }

    public List<LevelStartedMsg> LevelStarted { get; } = new();

    public List<LevelFinishedMsg> LevelFinished { get; } = new();

    public List<TierUpMsg> TierUps { get; } = new();

    public List<UpgradeChosenMsg> UpgradesChosen { get; } = new();

    public List<QuotaCompletedMsg> QuotasCompleted { get; } = new();

    public List<ProtectedHitMsg> ProtectedHits { get; } = new();

    public List<HarvestBatchedMsg> HarvestBatches { get; } = new();

    public List<LevelStateChangedMsg> StateChanges { get; } = new();

    public void Dispose()
    {
        _subscriptions.Unsubscribe();
        _messenger.Dispose();
    }
}
