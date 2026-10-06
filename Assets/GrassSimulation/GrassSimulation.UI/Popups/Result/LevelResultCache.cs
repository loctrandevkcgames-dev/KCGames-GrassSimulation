using System;
using System.Collections.Generic;
using EncosyTower.Common;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;

namespace GrassSimulation.UI
{
    public sealed class LevelResultCache : IDisposable
    {
        private readonly List<ISubscription> _subscriptions = new();

        private Option<LevelResult> _finished;
        private Option<Result<LevelSettlement, SettleError>> _settlement;
        private int _version;

        public LevelResultCache(
              MessageSubscriber.Subscriber<GameplayScope> gameplaySubscriber
            , MessageSubscriber.Subscriber<ProgressionScope> progressionSubscriber
        )
        {
            _subscriptions.Add(LevelFinishedMsg.Subscribe(in gameplaySubscriber, OnLevelFinished));
            _subscriptions.Add(LevelStateChangedMsg.Subscribe(in gameplaySubscriber, OnLevelStateChanged));
            _subscriptions.Add(LevelSettledMsg.Subscribe(in progressionSubscriber, OnLevelSettled));
        }

        public LastLevelResult GetLast()
        {
            return new LastLevelResult(_version, _finished, _settlement);
        }

        public void Dispose()
        {
            _subscriptions.Unsubscribe();
        }

        private void OnLevelFinished(LevelFinishedMsg message)
        {
            _finished = message.Result;
            _version++;
        }

        private void OnLevelSettled(LevelSettledMsg message)
        {
            _settlement = message.Outcome;
            _version++;
        }

        private void OnLevelStateChanged(LevelStateChangedMsg message)
        {
            var isRestarting = message.State == LevelState.Preview || message.State == LevelState.Playing;

            if (isRestarting == false || (_finished.HasValue == false && _settlement.HasValue == false))
            {
                return;
            }

            _finished = default;
            _settlement = default;
            _version++;
        }
    }
}
