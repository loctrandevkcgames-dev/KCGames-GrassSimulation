using EncosyTower.PageFlows.MonoPages;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class MainMenuScreen : MonoPageBase<GrassPageFlowScopes>
    {
        private const float POLL_INTERVAL = 0.1f;

        [SerializeField]
        private TMP_Text _coinText;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _gardenText;

        [SerializeField]
        private TMP_Text _progressTitleText;

        [SerializeField]
        private TMP_Text _progressText;

        [SerializeField]
        private RectTransform _progressFill;

        [SerializeField]
        private MainMenuGardenGrid _gardenGrid;

        [SerializeField]
        private TMP_Text _nextHeaderText;

        [SerializeField]
        private TMP_Text _nextTitleText;

        [SerializeField]
        private TMP_Text _timerText;

        [SerializeField]
        private MainMenuQuotaChip[] _quotaChips;

        [SerializeField]
        private PlantIconSet _plantIcons;

        [SerializeField]
        private Button _playButton;

        [SerializeField]
        private TMP_Text _playLabel;

        [SerializeField]
        private MainMenuLockedButton _zenButton;

        [SerializeField]
        private MainMenuLockedButton _levelSelectButton;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private Processor.Hub<GameplayScope> _gameplayHub;
        private Processor.Hub<ProgressionScope> _progressionHub;
        private ProcessingContext _processingContext;
        private ProgressSnapshot _progress;
        private float _pollTimer;
        private int _previewIndex;
        private bool _hasProgress;
        private bool _hasPreview;

        private void Awake()
        {
            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();
            _gameplayHub = GlobalProcessor.Instance.Scope<GameplayScope>();
            _progressionHub = GlobalProcessor.Instance.Scope<ProgressionScope>();
            _processingContext = ProcessingContext.DropIfNoHandler(warnNoHandler: false);

            _titleText.text = UiText.GAME_TITLE;
            _gardenText.text = UiText.GARDEN_NAME;
            _progressTitleText.text = UiText.GARDEN_PROGRESS_TITLE;
            _playLabel.text = UiText.BUTTON_PLAY;

            _zenButton.Init(UiText.BUTTON_ZEN);
            _levelSelectButton.Init(UiText.BUTTON_LEVEL_SELECT);

            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnEnable()
        {
            _hasProgress = false;
            _hasPreview = false;
            _pollTimer = 0f;

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

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            UiAudio.Tap();
            PlayRequestedMsg.Publish(in _commands, new PlayRequestedMsg());
        }

        private void Refresh()
        {
            var result = GetProgressSnapshotRequest.TryProcess(
                  in _progressionHub
                , new GetProgressSnapshotRequest()
                , _processingContext
            );

            if (result.TryGetValue(out var progress) == false)
            {
                return;
            }

            ShowProgress(in progress);
            ShowNextLevel(in progress);
        }

        private void ShowProgress(in ProgressSnapshot progress)
        {
            if (_hasProgress && _progress == progress)
            {
                return;
            }

            _hasProgress = true;
            _progress = progress;

            var completedCount = progress.CompletedCount;
            var levelCount = progress.LevelCount;
            var fraction = MainMenuScreenFormat.GetProgressFraction(completedCount, levelCount);

            _coinText.text = MainMenuScreenFormat.FormatCoins(progress.Coins);
            _progressText.text = MainMenuScreenFormat.FormatProgress(completedCount, levelCount, progress.TotalStars);
            _progressFill.anchorMax = new Vector2(x: fraction, y: 1f);
            _nextHeaderText.text = MainMenuScreenFormat.FormatNextHeader(completedCount, levelCount);
            _gardenGrid.Apply(in progress);
        }

        private void ShowNextLevel(in ProgressSnapshot progress)
        {
            var index = progress.NextLevelIndex;

            if (_hasPreview && _previewIndex == index)
            {
                return;
            }

            var result = GetLevelPreviewRequest.TryProcess(
                  in _gameplayHub
                , new GetLevelPreviewRequest(index)
                , _processingContext
            );

            if (result.TryGetValue(out var preview) == false)
            {
                return;
            }

            _hasPreview = true;
            _previewIndex = index;

            _nextTitleText.text = MainMenuScreenFormat.FormatLevelNumber(preview.LevelIndex);
            _timerText.text = GameplayScreenFormat.FormatTimer(preview.TimeLimit);

            ShowQuotas(in preview);
        }

        private void ShowQuotas(in LevelPreview preview)
        {
            var chipIndex = 0;
            var quotaCount = preview.QuotaCount;

            for (var i = 0; i < quotaCount && chipIndex < _quotaChips.Length; i++)
            {
                var quota = preview.GetQuota(i);

                if (quota.IsBonus)
                {
                    continue;
                }

                var chip = _quotaChips[chipIndex++];

                chip.gameObject.SetActive(true);
                chip.Apply(in quota, _plantIcons.Get(quota.Kind));
            }

            for (var i = chipIndex; i < _quotaChips.Length; i++)
            {
                _quotaChips[i].gameObject.SetActive(false);
            }
        }
    }
}
