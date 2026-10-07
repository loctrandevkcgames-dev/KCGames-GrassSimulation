using System;
using System.Collections.Generic;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public sealed class BoosterConsumptionHandler : IDisposable
    {
        private readonly ProgressionService _service;
        private readonly List<ISubscription> _subscriptions = new();

        public BoosterConsumptionHandler(
              ProgressionService service
            , MessageSubscriber.Subscriber<GameplayScope> subscriber
        )
        {
            _service = service;
            _subscriptions.Add(BoosterActivatedMsg.Subscribe(in subscriber, OnBoosterActivated));
        }

        public void Dispose()
        {
            _subscriptions.Unsubscribe();
        }

        private void OnBoosterActivated(BoosterActivatedMsg message)
        {
            var consumed = _service.ConsumeBooster(message.Kind);

            if (consumed.TryGetFailure(out var failure))
            {
                ThrowHelper.LogError_BoosterNotConsumed(message.Kind, failure);
            }
        }
    }
}
