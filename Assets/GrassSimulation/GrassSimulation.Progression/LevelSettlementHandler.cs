using System;
using System.Collections.Generic;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public sealed class LevelSettlementHandler : IDisposable
    {
        private readonly ProgressionService _service;
        private readonly MessagePublisher.Publisher<ProgressionScope> _publisher;
        private readonly List<ISubscription> _subscriptions = new();
        private readonly List<PendingSettlement> _pending = new();

        private bool _isRetrying;

        public LevelSettlementHandler(
              ProgressionService service
            , MessageSubscriber.Subscriber<GameplayScope> subscriber
            , MessagePublisher.Publisher<ProgressionScope> publisher
        )
        {
            _service = service;
            _publisher = publisher;
            _subscriptions.Add(LevelFinishedMsg.Subscribe(in subscriber, OnLevelFinished));
        }

        public bool HasPending => _pending.Count > 0;

        public void RetryPending()
        {
            if (_isRetrying)
            {
                return;
            }

            _isRetrying = true;

            try
            {
                while (_pending.Count > 0)
                {
                    var entry = _pending[0];
                    var result = entry.Result;
                    var outcome = _service.Settle(entry.Level, in result);

                    if (outcome.IsError == false)
                    {
                        _pending.RemoveAt(0);
                    }

                    LevelSettledMsg.Publish(in _publisher, new LevelSettledMsg(entry.Level, outcome));

                    if (outcome.IsError)
                    {
                        return;
                    }
                }
            }
            finally
            {
                _isRetrying = false;
            }
        }

        public void ClearPending()
        {
            _pending.Clear();
        }

        public void Dispose()
        {
            _subscriptions.Unsubscribe();
            _pending.Clear();
        }

        private void OnLevelFinished(LevelFinishedMsg message)
        {
            _pending.Add(new PendingSettlement(message.Level, message.Result));
            RetryPending();
        }

        private readonly record struct PendingSettlement(LevelId Level, LevelResult Result);
    }
}
