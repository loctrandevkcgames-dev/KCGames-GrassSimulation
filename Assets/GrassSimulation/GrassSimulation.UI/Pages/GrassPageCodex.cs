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
        private string _wantedScreenKey;
        private string _shownPopupKey;
        private string _wantedPopupKey;
        private LevelState _state;
        private bool _isPaused;
        private bool _isHome = true;
        private bool _isSettingsOpen;
        private bool _isScreenBusy;
        private bool _isPopupBusy;

        public IPageFlowScopeCollectionApplier PageFlowScopeCollectionApplier => _flowScopesApplier;

        public UnityTask OnInitializeAsync(MonoPageCodex codex)
        {
            _codex = codex;

            var subscriber = GlobalMessenger.Subscriber.Scope<GameplayScope>();

            _subscriptions.Add(LevelStateChangedMsg.Subscribe(in subscriber, OnLevelStateChanged));
            _subscriptions.Add(HomeChangedMsg.Subscribe(in subscriber, OnHomeChanged));

            var uiSubscriber = GlobalMessenger.Subscriber.Scope<UiScope>();

            _subscriptions.Add(SettingsRequestedMsg.Subscribe(in uiSubscriber, OnSettingsRequested));

            var hub = GlobalProcessor.Instance.Scope<GameplayScope>();
            var context = ProcessingContext.DropIfNoHandler(warnNoHandler: false);
            var result = GetLevelSnapshotRequest.TryProcess(in hub, new GetLevelSnapshotRequest(), context);

            if (result.TryGetValue(out var snapshot))
            {
                _state = snapshot.State;
                _isPaused = snapshot.IsPaused;

                Route();
            }

            return UnityTask.CompletedTask;
        }

        private void OnDestroy()
        {
            _subscriptions.Unsubscribe();
        }

        private void OnLevelStateChanged(LevelStateChangedMsg message)
        {
            _state = message.State;
            _isPaused = message.IsPaused;

            Route();
        }

        private void OnHomeChanged(HomeChangedMsg message)
        {
            _isHome = message.IsHome;
            _isSettingsOpen = false;

            Route();
        }

        private void OnSettingsRequested(SettingsRequestedMsg message)
        {
            _isSettingsOpen = message.IsOpen && _isHome;

            Route();
        }

        private void Route()
        {
            RoutePopup();

            if (GrassPageRoutes.TryGetScreenKey(_state, _isHome, out var key) == false)
            {
                return;
            }

            _wantedScreenKey = key;

            if (_isScreenBusy == false && key != _shownKey)
            {
                _ = SyncScreenAsync();
            }
        }

        private async UnityTask SyncScreenAsync()
        {
            if (_flowScopesApplier.TryGet(out var scopes) == false)
            {
                return;
            }

            _isScreenBusy = true;

            try
            {
                var publisher = _codex.FlowContext.Publisher.Scope(scopes.Screen);

                while (_wantedScreenKey != _shownKey)
                {
                    var key = _wantedScreenKey;

                    await ShowPageMessage.Async.Publish(
                          in publisher
                        , new ShowPageMessage(key, new PageContext {
                            ShowOptions = PageTransitionOptions.NoTransition,
                        })
                    );

                    _shownKey = key;
                }
            }
            finally
            {
                _isScreenBusy = false;
            }
        }

        private void RoutePopup()
        {
            GrassPageRoutes.TryGetPopupKey(_state, _isPaused, _isHome, _isSettingsOpen, out var key);

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
                    if (GrassPageRoutes.TryGetPopupSound(_shownPopupKey, _wantedPopupKey, out var sound))
                    {
                        UiAudio.Request(sound);
                    }

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
