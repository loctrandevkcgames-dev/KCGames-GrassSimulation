using System;
using System.Collections.Generic;
using EncosyTower.PubSub;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class GameHaptics : IDisposable
    {
        private const float MIN_TICK_INTERVAL = 0.08f;

        private readonly List<ISubscription> _subscriptions = new();
        private readonly DeviceHaptics _device = new();

        private bool _isEnabled;
        private float _lastTickTime = float.NegativeInfinity;

        public GameHaptics(MessageSubscriber.Subscriber<GameplayScope> subscriber)
        {
            _isEnabled = PlayerOptions.GetHaptics();

            _subscriptions.Add(HarvestBatchedMsg.Subscribe(in subscriber, OnHarvestBatched));
            _subscriptions.Add(PlantHarvestedMsg.Subscribe(in subscriber, OnPlantHarvested));
            _subscriptions.Add(QuotaCompletedMsg.Subscribe(in subscriber, OnQuotaCompleted));
            _subscriptions.Add(TierUpMsg.Subscribe(in subscriber, OnTierUp));
            _subscriptions.Add(ProtectedHitMsg.Subscribe(in subscriber, OnProtectedHit));
            _subscriptions.Add(LevelFinishedMsg.Subscribe(in subscriber, OnLevelFinished));
            _subscriptions.Add(PlayerOptionsChangedMsg.Subscribe(in subscriber, OnPlayerOptionsChanged));
        }

        public void Dispose()
        {
            _subscriptions.Unsubscribe();
            _device.Dispose();
        }

        private void Play(HapticPulse pulse)
        {
            if (_isEnabled)
            {
                _device.Play(pulse);
            }
        }

        private void OnHarvestBatched(HarvestBatchedMsg message)
        {
            var now = Time.unscaledTime;

            if (message.Cells <= 0 || now - _lastTickTime < MIN_TICK_INTERVAL)
            {
                return;
            }

            _lastTickTime = now;
            Play(HapticPulse.Tick);
        }

        private void OnPlantHarvested(PlantHarvestedMsg message)
        {
            Play(message.IsFruit ? HapticPulse.Medium : HapticPulse.Light);
        }

        private void OnQuotaCompleted(QuotaCompletedMsg message)
        {
            Play(HapticPulse.Medium);
        }

        private void OnTierUp(TierUpMsg message)
        {
            Play(HapticPulse.Heavy);
        }

        private void OnProtectedHit(ProtectedHitMsg message)
        {
            Play(HapticPulse.Medium);
        }

        private void OnLevelFinished(LevelFinishedMsg message)
        {
            Play(HapticPulse.Heavy);
        }

        private void OnPlayerOptionsChanged(PlayerOptionsChangedMsg message)
        {
            _isEnabled = PlayerOptions.GetHaptics();
        }
    }
}
