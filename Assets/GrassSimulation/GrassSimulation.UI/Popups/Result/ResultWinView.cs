using EncosyTower.Common;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class ResultWinView : MonoBehaviour
    {
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
        [FormerlySerializedAs("_timeRow")]
        private ResultRow _flawlessRow;

        [SerializeField]
        [FormerlySerializedAs("_cleanRow")]
        private ResultRow _sideRow;

        [SerializeField]
        private GameObject _assistedCard;

        [SerializeField]
        private TMP_Text _assistedText;

        [SerializeField]
        private GameObject _unlockCard;

        [SerializeField]
        private TMP_Text _unlockText;

        [SerializeField]
        [FormerlySerializedAs("_coinLinesText")]
        private TMP_Text _saveStatusText;

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

        public void Init(in MessagePublisher.Publisher<LevelCommandScope> commands)
        {
            _commands = commands;

            _titleText.text = UiText.RESULT_TITLE;
            _assistedText.text = UiText.RESULT_ASSISTED;
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
            var stars = result.Stars;

            for (var i = 0; i < _stars.Length; i++)
            {
                var star = (StarFlags)(1 << i);

                _stars[i].sprite = ResultPopupFormat.HasStar(stars, star) ? _starOnSprite : _starOffSprite;
            }

            var bonus = ResultPopupFormat.GetBonus(in snapshot);

            _goalRow.Apply(ResultPopupFormat.CreateGoalRow());
            _flawlessRow.Apply(ResultPopupFormat.CreateCleanRow(in result, in snapshot));
            _sideRow.Apply(ResultPopupFormat.CreateSideRow(in result, in snapshot, in bonus));

            _assistedCard.SetActive(result.IsAssisted);
            _nextButton.gameObject.SetActive(ResultPopupFormat.HasNextLevel(snapshot.LevelIndex, snapshot.LevelCount));

            ShowSaveStatus(in settlement);
            ShowUnlocks(in settlement);
        }

        private void ShowUnlocks(in Option<Result<LevelSettlement, SettleError>> settlement)
        {
            var hasUnlocks = false;
            var text = string.Empty;

            if (settlement.TryGetValue(out var outcome) && outcome.TryGetValue(out var value))
            {
                hasUnlocks = LoadoutScreenFormat.TryFormatUnlocks(value.Unlocks, out text);
            }

            _unlockCard.SetActive(hasUnlocks);

            if (hasUnlocks)
            {
                _unlockText.text = text;
            }
        }

        private void ShowSaveStatus(in Option<Result<LevelSettlement, SettleError>> settlement)
        {
            _retrySaveButton.gameObject.SetActive(false);

            if (settlement.TryGetValue(out var outcome) == false)
            {
                ShowStatus(UiText.SAVE_SAVING, UiPalette.Muted);
                return;
            }

            if (outcome.IsError == false)
            {
                ShowStatus(string.Empty, UiPalette.Muted);
                return;
            }

            outcome.TryGetError(out var error);

            ShowStatus(UiText.SAVE_FAILED, UiPalette.Warning);
            _retrySaveButton.gameObject.SetActive(error.CanRetry);
        }

        private void ShowStatus(string text, Color color)
        {
            _saveStatusText.text = text;
            _saveStatusText.color = color;
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
