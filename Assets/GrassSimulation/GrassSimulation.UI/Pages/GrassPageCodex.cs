using System.Collections.Generic;
using EncosyTower.PageFlows;
using EncosyTower.PageFlows.MonoPages;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using EncosyTower.Tasks;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class GrassPageCodex : MonoBehaviour, IMonoPageCodexOnInitialize
    {
        private readonly PageFlowScopeCollectionApplier<GrassPageFlowScopes> _flowScopesApplier = new();
        private readonly List<ISubscription> _subscriptions = new();

        private MonoPageCodex _codex;
        private string _shownKey;
        private string _pendingKey;
        private string _shownPopupKey;
        private string _wantedPopupKey;
        private bool _isPopupBusy;

        public IPageFlowScopeCollectionApplier PageFlowScopeCollectionApplier => _flowScopesApplier;

        public UnityTask OnInitializeAsync(MonoPageCodex codex)
        {
            _codex = codex;

            var subscriber = GlobalMessenger.Subscriber.Scope<GameplayScope>();

            _subscriptions.Add(LevelStateChangedMsg.Subscribe(in subscriber, OnLevelStateChanged));

            var hub = GlobalProcessor.Instance.Scope<GameplayScope>();
            var context = ProcessingContext.DropIfNoHandler(warnNoHandler: false);
            var result = GetLevelSnapshotRequest.TryProcess(in hub, new GetLevelSnapshotRequest(), context);

            if (result.TryGetValue(out var snapshot))
            {
                Route(snapshot.State, snapshot.IsPaused);
            }

            return UnityTask.CompletedTask;
        }

        private void OnDestroy()
        {
            _subscriptions.Unsubscribe();
        }

        private void OnLevelStateChanged(LevelStateChangedMsg message)
        {
            Route(message.State, message.IsPaused);
        }

        private void Route(LevelState state, bool isPaused)
        {
            RoutePopup(state, isPaused);

            if (GrassPageRoutes.TryGetScreenKey(state, out var key) == false
                || key == _shownKey
                || key == _pendingKey
                || _flowScopesApplier.TryGet(out var scopes) == false
            )
            {
                return;
            }

            _ = ShowAsync(key, scopes);
        }

        private async UnityTask ShowAsync(string key, GrassPageFlowScopes scopes)
        {
            _pendingKey = key;

            try
            {
                var publisher = _codex.FlowContext.Publisher.Scope(scopes.Screen);

                await ShowPageMessage.Async.Publish(
                      in publisher
                    , new ShowPageMessage(key, new PageContext {
                        ShowOptions = PageTransitionOptions.NoTransition,
                    })
                );

                _shownKey = key;
            }
            finally
            {
                _pendingKey = null;
            }
        }

        private void RoutePopup(LevelState state, bool isPaused)
        {
            GrassPageRoutes.TryGetPopupKey(state, isPaused, out var key);

            _wantedPopupKey = key;

            if (_isPopupBusy == false && key != _shownPopupKey)
            {
                _ = SyncPopupAsync();
            }
        }

        private async UnityTask SyncPopupAsync()
        {
            if (_flowScopesApplier.TryGet(out var scopes) == false)
            {
                return;
            }

            _isPopupBusy = true;

            try
            {
                var publisher = _codex.FlowContext.Publisher.Scope(scopes.Popup);

                while (_wantedPopupKey != _shownPopupKey)
                {
                    var context = new PageContext {
                        ShowOptions = PageTransitionOptions.NoTransition,
                        HideOptions = PageTransitionOptions.NoTransition,
                    };

                    if (_shownPopupKey != null)
                    {
                        await HideActivePageMessage.Async.Publish(in publisher, new HideActivePageMessage(context));

                        _shownPopupKey = null;
                    }
                    else
                    {
                        var key = _wantedPopupKey;

                        await ShowPageMessage.Async.Publish(in publisher, new ShowPageMessage(key, context));

                        _shownPopupKey = key;
                    }
                }
            }
            finally
            {
                _isPopupBusy = false;
            }
        }
    }
}
