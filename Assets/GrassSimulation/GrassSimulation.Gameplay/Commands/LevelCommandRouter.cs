using System;
using System.Collections.Generic;
using EncosyTower.PubSub;

namespace GrassSimulation.Gameplay
{
    public sealed class LevelCommandRouter : IDisposable
    {
        private readonly ILevelFlowHost _host;
        private readonly List<ISubscription> _subscriptions = new();

        public LevelCommandRouter(MessageSubscriber.Subscriber<LevelCommandScope> subscriber, ILevelFlowHost host)
        {
            _host = host;

            _subscriptions.Add(StartRequestedMsg.Subscribe(in subscriber, OnStart));
            _subscriptions.Add(UpgradeRequestedMsg.Subscribe(in subscriber, OnUpgrade));
            _subscriptions.Add(PauseRequestedMsg.Subscribe(in subscriber, OnPause));
            _subscriptions.Add(RetryRequestedMsg.Subscribe(in subscriber, OnRetry));
            _subscriptions.Add(NextLevelRequestedMsg.Subscribe(in subscriber, OnNextLevel));
            _subscriptions.Add(CleanupRequestedMsg.Subscribe(in subscriber, OnCleanup));
            _subscriptions.Add(FinishCleanupRequestedMsg.Subscribe(in subscriber, OnFinishCleanup));
            _subscriptions.Add(QuitRequestedMsg.Subscribe(in subscriber, OnQuit));
            _subscriptions.Add(PlayRequestedMsg.Subscribe(in subscriber, OnPlay));
            _subscriptions.Add(RetrySaveRequestedMsg.Subscribe(in subscriber, OnRetrySave));
        }

        public void Dispose()
        {
            _subscriptions.Unsubscribe();
        }

        private void OnStart(StartRequestedMsg message)
        {
            _host.Begin();
        }

        private void OnUpgrade(UpgradeRequestedMsg message)
        {
            _host.Session?.TryChooseUpgrade(message.Option);
        }

        private void OnPause(PauseRequestedMsg message)
        {
            var session = _host.Session;

            if (session == null)
            {
                return;
            }

            if (message.Paused)
            {
                session.Pause();
            }
            else
            {
                session.Resume();
            }
        }

        private void OnCleanup(CleanupRequestedMsg message)
        {
            _host.Session?.TryEnterCleanup();
        }

        private void OnRetry(RetryRequestedMsg message)
        {
            _host.Retry();
        }

        private void OnNextLevel(NextLevelRequestedMsg message)
        {
            _host.LoadNext();
        }

        private void OnFinishCleanup(FinishCleanupRequestedMsg message)
        {
            _host.FinishCleanup();
        }

        private void OnQuit(QuitRequestedMsg message)
        {
            _host.GoHome();
        }

        private void OnPlay(PlayRequestedMsg message)
        {
            _host.Play();
        }

        private void OnRetrySave(RetrySaveRequestedMsg message)
        {
            _host.RetrySave();
        }
    }
}
