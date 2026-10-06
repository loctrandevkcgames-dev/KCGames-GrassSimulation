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
        private const float TOAST_SECONDS = 4f;

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
        private RectTransform _joystickLayer;

        [SerializeField]
        private RectTransform _joystickBase;

        [SerializeField]
        private RectTransform _joystickKnob;

        [SerializeField]
        private GameObject _hint;

        [SerializeField]
        private TMP_Text _hintText;

        [SerializeField]
        private Sprite _grassIcon;

        [SerializeField]
        private Sprite _flowerIcon;

        [SerializeField]
        private Sprite _bushIcon;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private Processor.Hub<GameplayScope> _hub;
        private ProcessingContext _processingContext;
        private readonly OnboardingHintModel _hintModel = new();

        private float _pollTimer;
        private int _levelIndex;
        private LevelState _levelState;
        private bool _isPaused;
        private bool _isJoystickShown;

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
        private bool _isProtectedFailMode;
        private QuotaSnapshot _bonus;
        private bool _hasBonus;
        private bool _isCleanup;
        private bool _isTimed;
        private bool _hasMode;
        private float _timerWarningSeconds;
        private float _toastSeconds;
        private int _clearedPercent;
        private bool _hasClearedPercent;

        private void Awake()
        {
            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();
            _hub = GlobalProcessor.Instance.Scope<GameplayScope>();
            _processingContext = ProcessingContext.DropIfNoHandler(warnNoHandler: false);

            _finishLabel.text = UiText.FINISH_CLEANUP;
            _hintText.text = UiText.DRAG_HINT;

            _pauseButton.onClick.AddListener(OnPauseClicked);
            _finishButton.onClick.AddListener(OnFinishClicked);
        }

        private void OnEnable()
        {
            ResetCaches();
            _hintModel.Reset();
            _hint.SetActive(false);
            HideJoystick();
            Refresh();

            _pollTimer = 0f;
        }

        private void Update()
        {
            ShowJoystickAndHint();

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
            UiAudio.Tap();
            PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(Paused: true));
        }

        private void OnFinishClicked()
        {
            UiAudio.Tap();
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
            _toastSeconds = 0f;

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

            _levelIndex = snapshot.LevelIndex;
            _levelState = snapshot.State;
            _isPaused = snapshot.IsPaused;

            ShowMode(snapshot);
            ShowTimer(snapshot);
            ShowQuotas(snapshot);
            ShowGrowth(snapshot);
            ShowProtectedHits(snapshot);
        }

        private void ShowJoystickAndHint()
        {
            var result = GetJoystickStateRequest.TryProcess(in _hub, new GetJoystickStateRequest(), _processingContext);

            if (result.TryGetValue(out var joystick) == false)
            {
                joystick = default;
            }

            ShowJoystick(in joystick);

            var isActive = joystick.IsDragging || joystick.IsMoving;

            _hintModel.Update(Time.unscaledDeltaTime, _levelIndex, _levelState, _isPaused, isActive);

            var isToastShown = _toastSeconds > 0f;
            var isHintShown = isToastShown || _hintModel.IsVisible;

            if (isToastShown)
            {
                _toastSeconds -= Time.unscaledDeltaTime;

                if (_toastSeconds <= 0f)
                {
                    _hintText.text = UiText.DRAG_HINT;
                    isHintShown = _hintModel.IsVisible;
                }
            }

            if (_hint.activeSelf != isHintShown)
            {
                _hint.SetActive(isHintShown);
            }
        }

        private void ShowJoystick(in JoystickState joystick)
        {
            if (joystick.IsDragging == false)
            {
                HideJoystick();
                return;
            }

            if (_isJoystickShown == false)
            {
                _isJoystickShown = true;
                _joystickBase.gameObject.SetActive(true);
                _joystickKnob.gameObject.SetActive(true);
            }

            var layerSize = _joystickLayer.rect.size;
            var screenSize = new Vector2(Screen.width, Screen.height);
            var unitsPerPixel = JoystickLayout.GetUnitsPerPixel(layerSize.x, screenSize.x);
            var origin = JoystickLayout.ToCanvasPosition(joystick.Origin, screenSize, layerSize);
            var baseSize = JoystickLayout.GetBaseSize(joystick.Radius, unitsPerPixel);
            var knobSize = JoystickLayout.GetKnobSize(joystick.Radius, unitsPerPixel);

            _joystickBase.anchoredPosition = origin;
            _joystickBase.sizeDelta = new Vector2(baseSize, baseSize);
            _joystickKnob.anchoredPosition = origin + JoystickLayout.GetKnobOffset(
                  joystick.Input
                , joystick.Radius
                , unitsPerPixel
            );
            _joystickKnob.sizeDelta = new Vector2(knobSize, knobSize);
        }

        private void HideJoystick()
        {
            if (_isJoystickShown == false && _joystickBase.gameObject.activeSelf == false)
            {
                return;
            }

            _isJoystickShown = false;
            _joystickBase.gameObject.SetActive(false);
            _joystickKnob.gameObject.SetActive(false);
        }

        private void ShowMode(in LevelSnapshot snapshot)
        {
            var isCleanup = snapshot.State == LevelState.Cleanup;
            var isTimed = snapshot.IsTimed;

            _timerWarningSeconds = snapshot.TimerWarning;

            if (_hasMode == false || isCleanup != _isCleanup || isTimed != _isTimed)
            {
                var isCleanupEntered = isCleanup && (_hasMode == false || _isCleanup == false);

                _hasMode = true;
                _isCleanup = isCleanup;
                _isTimed = isTimed;
                _timer.SetActive(isCleanup == false && isTimed);
                _clearedPill.SetActive(isCleanup);
                _finishButton.gameObject.SetActive(isCleanup);

                if (isCleanupEntered)
                {
                    ShowToast(GameplayScreenFormat.FormatCleanupToast(snapshot.CleanupTier));
                }
            }

            if (isCleanup)
            {
                ShowCleared(snapshot.ClearedFraction);
            }
        }

        private void ShowToast(string text)
        {
            _toastSeconds = TOAST_SECONDS;
            _hintText.text = text;
            _hint.SetActive(true);
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
            if (_isCleanup || _isTimed == false)
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

            var isWarning = GameplayScreenFormat.IsTimerWarning(remaining, _timerWarningSeconds);

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
            var hasBeds = snapshot.HasProtectedBeds;

            _protectedPill.SetActive(hasBeds);

            var isFailMode = snapshot.FailsOnProtectedHits;
            var isSame = snapshot.ProtectedHits == _protectedHits && isFailMode == _isProtectedFailMode;

            if (hasBeds == false || (_hasProtectedHits && isSame))
            {
                return;
            }

            _hasProtectedHits = true;
            _protectedHits = snapshot.ProtectedHits;
            _isProtectedFailMode = isFailMode;
            _protectedText.text = GameplayScreenFormat.FormatHits(
                  snapshot.ProtectedHits
                , snapshot.ProtectedHitLimit
                , isFailMode
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
