using EncosyTower.Common;
using EncosyTower.PubSub;
using GrassSimulation.Audio;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class ResultWinView : MonoBehaviour
    {
        private const int COIN_SOUND_COUNT = 3;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private Image[] _stars;

        [SerializeField]
        private Sprite _starOnSprite;

        [SerializeField]
        private Sprite _starOffSprite;

        [SerializeField]
        private ResultRow _goalRow;

        [SerializeField]
        private ResultRow _timeRow;

        [SerializeField]
        private ResultRow _cleanRow;

        [SerializeField]
        private TMP_Text _coinLinesText;

        [SerializeField]
        private GameObject _coinTotal;

        [SerializeField]
        private TMP_Text _coinTotalText;

        [SerializeField]
        private Button _retrySaveButton;

        [SerializeField]
        private TMP_Text _retrySaveLabel;

        [SerializeField]
        private Button _nextButton;

        [SerializeField]
        private TMP_Text _nextLabel;

        [SerializeField]
        private Button _replayButton;

        [SerializeField]
        private TMP_Text _replayLabel;

        [SerializeField]
        private Button _cleanupButton;

        [SerializeField]
        private TMP_Text _cleanupLabel;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private bool _coinsPlayed;

        public void Init(in MessagePublisher.Publisher<LevelCommandScope> commands)
        {
            _commands = commands;

            _titleText.text = UiText.RESULT_TITLE;
            _retrySaveLabel.text = UiText.BUTTON_RETRY_SAVE;
            _nextLabel.text = UiText.BUTTON_NEXT;
            _replayLabel.text = UiText.BUTTON_REPLAY;
            _cleanupLabel.text = UiText.BUTTON_CLEANUP;

            _retrySaveButton.onClick.AddListener(OnRetrySaveClicked);
            _nextButton.onClick.AddListener(OnNextClicked);
            _replayButton.onClick.AddListener(OnReplayClicked);
            _cleanupButton.onClick.AddListener(OnCleanupClicked);
        }

        public void Apply(
              in LevelResult result
            , in LevelSnapshot snapshot
            , in Option<Result<LevelSettlement, SettleError>> settlement
        )
        {
            result.TryGetStars(out var stars);

            for (var i = 0; i < _stars.Length; i++)
            {
                _stars[i].sprite = i < stars ? _starOnSprite : _starOffSprite;
            }

            var bonus = ResultPopupFormat.GetBonus(in snapshot);

            _goalRow.Apply(ResultPopupFormat.CreateGoalRow());
            _timeRow.Apply(ResultPopupFormat.CreateTimeRow(result.RemainingTime, snapshot.TimeLimit));
            _cleanRow.Apply(ResultPopupFormat.CreateCleanRow(result.ProtectedHits, in bonus));

            _nextButton.gameObject.SetActive(ResultPopupFormat.HasNextLevel(snapshot.LevelIndex, snapshot.LevelCount));

            ShowCoins(in settlement);
        }

        private void OnEnable()
        {
            _coinsPlayed = false;
        }

        private void ShowCoins(in Option<Result<LevelSettlement, SettleError>> settlement)
        {
            _coinTotal.SetActive(false);
            _retrySaveButton.gameObject.SetActive(false);

            if (settlement.TryGetValue(out var outcome) == false)
            {
                ShowCoinLines(UiText.COINS_SAVING, UiPalette.CoinInk);
                return;
            }

            if (outcome.TryGetValue(out var settled))
            {
                ShowSettled(in settled);
                return;
            }

            outcome.TryGetError(out var error);

            ShowCoinLines(UiText.COINS_SAVE_FAILED, UiPalette.Warning);
            _retrySaveButton.gameObject.SetActive(error.CanRetry);
        }

        private void ShowSettled(in LevelSettlement settled)
        {
            ShowCoinLines(ResultPopupFormat.FormatCoinBreakdown(in settled), UiPalette.CoinInk);

            if (settled.CoinsGranted <= 0)
            {
                return;
            }

            _coinTotal.SetActive(true);
            _coinTotalText.text = ResultPopupFormat.FormatCoinTotal(settled.CoinsGranted);

            if (_coinsPlayed == false)
            {
                _coinsPlayed = true;
                UiAudio.Request(UiSound.Coins, COIN_SOUND_COUNT);
            }
        }

        private void ShowCoinLines(string text, Color color)
        {
            _coinLinesText.text = text;
            _coinLinesText.color = color;
        }

        private void OnRetrySaveClicked()
        {
            UiAudio.Tap();
            RetrySaveRequestedMsg.Publish(in _commands, new RetrySaveRequestedMsg());
        }

        private void OnNextClicked()
        {
            UiAudio.Tap();
            NextLevelRequestedMsg.Publish(in _commands, new NextLevelRequestedMsg());
        }

        private void OnReplayClicked()
        {
            UiAudio.Tap();
            RetryRequestedMsg.Publish(in _commands, new RetryRequestedMsg());
        }

        private void OnCleanupClicked()
        {
            UiAudio.Tap();
            CleanupRequestedMsg.Publish(in _commands, new CleanupRequestedMsg());
        }
    }
}
