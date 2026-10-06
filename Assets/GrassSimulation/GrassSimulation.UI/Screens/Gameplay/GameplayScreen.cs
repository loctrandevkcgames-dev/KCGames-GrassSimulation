using EncosyTower.PageFlows.MonoPages;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class GameplayScreen : MonoPageBase<GrassPageFlowScopes>
    {
        private const float POLL_INTERVAL = 0.1f;

        [SerializeField]
        private GameplayViewModel _viewModel;

        [SerializeField]
        private GameplayQuotaChip[] _quotaChips;

        [SerializeField]
        private GameObject _timer;

        [SerializeField]
        private Image _timerBackground;

        [SerializeField]
        private Image _timerIcon;

        [SerializeField]
        private TMP_Text _timerText;

        [SerializeField]
        private Button _pauseButton;

        [SerializeField]
        private GameObject _clearedPill;

        [SerializeField]
        private TMP_Text _clearedText;

        [SerializeField]
        private Button _finishButton;

        [SerializeField]
        private TMP_Text _finishLabel;

        [SerializeField]
        private TMP_Text _tierText;

        [SerializeField]
        private RectTransform _xpFill;

        [SerializeField]
        private TMP_Text _xpText;

        [SerializeField]
        private GameObject _bonusPill;

        [SerializeField]
        private TMP_Text _bonusText;

        [SerializeField]
        private GameObject _protectedPill;

        [SerializeField]
        private TMP_Text _protectedText;

        [SerializeField]
        private Sprite _grassIcon;

        [SerializeField]
        private Sprite _flowerIcon;

        [SerializeField]
        private Sprite _bushIcon;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private Processor.Hub<GameplayScope> _hub;
        private ProcessingContext _processingContext;
        private float _pollTimer;

        private int _timerSeconds;
        private bool _hasTimerSeconds;
        private bool _timerWarning;
        private bool _hasTimerStyle;
        private int _tier;
        private bool _hasTier;
        private int _xp;
        private bool _hasXp;
        private float _xpFraction;
        private bool _hasXpFraction;
        private int _protectedHits;
        private bool _hasProtectedHits;
        private QuotaSnapshot _bonus;
        private bool _hasBonus;
        private bool _isCleanup;
        private bool _hasMode;
        private int _clearedPercent;
        private bool _hasClearedPercent;

        private void Awake()
        {
            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();
            _hub = GlobalProcessor.Instance.Scope<GameplayScope>();
            _processingContext = ProcessingContext.DropIfNoHandler(warnNoHandler: false);

            _finishLabel.text = UiText.FINISH_CLEANUP;

            _pauseButton.onClick.AddListener(OnPauseClicked);
            _finishButton.onClick.AddListener(OnFinishClicked);
        }

        private void OnEnable()
        {
            ResetCaches();
            Refresh();

            _pollTimer = 0f;
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
            _pauseButton.onClick.RemoveListener(OnPauseClicked);
            _finishButton.onClick.RemoveListener(OnFinishClicked);
        }

        private void OnPauseClicked()
        {
            PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(Paused: true));
        }

        private void OnFinishClicked()
        {
            FinishCleanupRequestedMsg.Publish(in _commands, new FinishCleanupRequestedMsg());
        }

        private void ResetCaches()
        {
            _hasTimerSeconds = false;
            _hasTimerStyle = false;
            _hasTier = false;
            _hasXp = false;
            _hasXpFraction = false;
            _hasProtectedHits = false;
            _hasBonus = false;
            _hasMode = false;
            _hasClearedPercent = false;

            for (var i = 0; i < _quotaChips.Length; i++)
            {
                _quotaChips[i].ResetCache();
            }
        }

        private void Refresh()
        {
            var result = GetLevelSnapshotRequest.TryProcess(in _hub, new GetLevelSnapshotRequest(), _processingContext);

            if (result.TryGetValue(out var snapshot) == false)
            {
                return;
            }

            ShowMode(snapshot);
            ShowTimer(snapshot);
            ShowQuotas(snapshot);
            ShowGrowth(snapshot);
            ShowProtectedHits(snapshot);
        }

        private void ShowMode(in LevelSnapshot snapshot)
        {
            var isCleanup = snapshot.State == LevelState.Cleanup;

            if (_hasMode == false || isCleanup != _isCleanup)
            {
                _hasMode = true;
                _isCleanup = isCleanup;
                _timer.SetActive(isCleanup == false);
                _clearedPill.SetActive(isCleanup);
                _finishButton.gameObject.SetActive(isCleanup);
            }

            if (isCleanup)
            {
                ShowCleared(snapshot.ClearedFraction);
            }
        }

        private void ShowCleared(float fraction)
        {
            var percent = GameplayScreenFormat.GetClearedPercent(fraction);

            if (_hasClearedPercent && percent == _clearedPercent)
            {
                return;
            }

            _hasClearedPercent = true;
            _clearedPercent = percent;
            _clearedText.text = GameplayScreenFormat.FormatCleared(percent);
        }

        private void ShowTimer(in LevelSnapshot snapshot)
        {
            if (_isCleanup)
            {
                return;
            }

            var remaining = snapshot.RemainingTime;
            var seconds = Mathf.CeilToInt(Mathf.Max(remaining, 0f));

            if (_hasTimerSeconds == false || seconds != _timerSeconds)
            {
                _hasTimerSeconds = true;
                _timerSeconds = seconds;
                _viewModel.TimerText = GameplayScreenFormat.FormatTimer(remaining);
            }

            var isWarning = GameplayScreenFormat.IsTimerWarning(remaining);

            if (_hasTimerStyle && isWarning == _timerWarning)
            {
                return;
            }

            _hasTimerStyle = true;
            _timerWarning = isWarning;
            _timerBackground.color = isWarning ? UiPalette.Warning : UiPalette.White;

            var ink = isWarning ? UiPalette.White : UiPalette.Ink;

            _timerIcon.color = ink;
            _timerText.color = ink;
        }

        private void ShowQuotas(in LevelSnapshot snapshot)
        {
            var chipIndex = 0;
            var hasBonus = false;
            var quotaCount = snapshot.QuotaCount;

            for (var i = 0; i < quotaCount; i++)
            {
                var quota = snapshot.GetQuota(i);

                if (quota.IsBonus)
                {
                    hasBonus = true;

                    if (_hasBonus == false || _bonus != quota)
                    {
                        _bonus = quota;
                        _hasBonus = true;
                        _bonusText.text = GameplayScreenFormat.FormatBonus(quota.Kind, quota.Progress, quota.Amount);
                    }

                    continue;
                }

                if (chipIndex < _quotaChips.Length)
                {
                    var chip = _quotaChips[chipIndex++];

                    chip.gameObject.SetActive(true);
                    chip.Apply(in quota, GetKindIcon(quota.Kind));
                }
            }

            for (var i = chipIndex; i < _quotaChips.Length; i++)
            {
                _quotaChips[i].gameObject.SetActive(false);
            }

            _hasBonus = hasBonus && _hasBonus;
            _bonusPill.SetActive(hasBonus);
        }

        private void ShowGrowth(in LevelSnapshot snapshot)
        {
            var tier = snapshot.Tier;

            if (_hasTier == false || tier != _tier)
            {
                _hasTier = true;
                _tier = tier;
                _tierText.text = GameplayScreenFormat.FormatTier(tier);
            }

            var xp = snapshot.Xp;
            var fraction = GameplayScreenFormat.GetXpFraction(xp, snapshot.TierFloorXp, snapshot.NextThresholdXp);

            if (_hasXp == false || xp != _xp)
            {
                _hasXp = true;
                _xp = xp;
                _xpText.text = GameplayScreenFormat.FormatXp(xp, snapshot.NextThresholdXp);
            }

            if (_hasXpFraction == false || Mathf.Approximately(fraction, _xpFraction) == false)
            {
                _hasXpFraction = true;
                _xpFraction = fraction;
                _xpFill.anchorMax = new Vector2(x: fraction, y: 1f);
            }
        }

        private void ShowProtectedHits(in LevelSnapshot snapshot)
        {
            var isCounting = snapshot.CountsProtectedHits;

            _protectedPill.SetActive(isCounting);

            if (isCounting == false || (_hasProtectedHits && snapshot.ProtectedHits == _protectedHits))
            {
                return;
            }

            _hasProtectedHits = true;
            _protectedHits = snapshot.ProtectedHits;
            _protectedText.text = GameplayScreenFormat.FormatHitsLeft(
                  snapshot.ProtectedHits
                , snapshot.ProtectedHitLimit
            );
        }

        private Sprite GetKindIcon(PlantKind kind)
        {
            return PlantVisuals.GetIcon(kind) switch {
                PlantIcon.Flower => _flowerIcon,
                PlantIcon.Bush => _bushIcon,
                _ => _grassIcon,
            };
        }
    }
}
