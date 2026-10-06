using EncosyTower.PageFlows.MonoPages;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using GrassSimulation.Audio;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class ResultPopup : MonoPageBase<GrassPageFlowScopes>
    {
        private const float POLL_INTERVAL = 0.1f;

        [SerializeField]
        private ResultWinView _winView;

        [SerializeField]
        private ResultFailureView _failureView;

        private Processor.Hub<UiScope> _resultHub;
        private Processor.Hub<GameplayScope> _gameplayHub;
        private ProcessingContext _processingContext;
        private float _pollTimer;
        private int _appliedVersion;
        private bool _hasApplied;

        private void Awake()
        {
            var commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();

            _resultHub = GlobalProcessor.Instance.Scope<UiScope>();
            _gameplayHub = GlobalProcessor.Instance.Scope<GameplayScope>();
            _processingContext = ProcessingContext.DropIfNoHandler(warnNoHandler: false);

            _winView.Init(in commands);
            _failureView.Init(in commands);
        }

        private void OnEnable()
        {
            _hasApplied = false;
            _pollTimer = 0f;

            _winView.gameObject.SetActive(false);
            _failureView.gameObject.SetActive(false);

            Refresh();
        }

        private void Update()
        {
            _pollTimer += Time.unscaledDeltaTime;

            if (_pollTimer < POLL_INTERVAL)
            {
                return;
            }

            _pollTimer = 0f;
            Refresh();
        }

        private void Refresh()
        {
            var lastResult = GetLastLevelResultRequest.TryProcess(
                  in _resultHub
                , new GetLastLevelResultRequest()
                , _processingContext
            );

            if (lastResult.TryGetValue(out var last) == false
                || last.Finished.TryGetValue(out var finished) == false
                || (_hasApplied && last.Version == _appliedVersion)
            )
            {
                return;
            }

            var snapshotResult = GetLevelSnapshotRequest.TryProcess(
                  in _gameplayHub
                , new GetLevelSnapshotRequest()
                , _processingContext
            );

            if (snapshotResult.TryGetValue(out var snapshot) == false)
            {
                return;
            }

            var isFirstShow = _hasApplied == false;

            _hasApplied = true;
            _appliedVersion = last.Version;

            if (isFirstShow && finished.TryGetStars(out var stars))
            {
                UiAudio.Request(UiSound.Stars, stars);
            }

            Show(in finished, in snapshot, in last);
        }

        private void Show(in LevelResult finished, in LevelSnapshot snapshot, in LastLevelResult last)
        {
            var isSuccess = finished.Outcome.IsSuccess;

            _winView.gameObject.SetActive(isSuccess);
            _failureView.gameObject.SetActive(isSuccess == false);

            if (isSuccess)
            {
                _winView.Apply(in finished, in snapshot, last.Settlement);
            }
            else
            {
                _failureView.Apply(in finished, in snapshot);
            }
        }
    }
}
