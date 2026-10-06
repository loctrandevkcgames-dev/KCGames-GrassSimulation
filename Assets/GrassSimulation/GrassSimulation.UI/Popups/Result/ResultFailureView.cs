using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class ResultFailureView : MonoBehaviour
    {
        [SerializeField]
        private Image _tile;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private Sprite _clockSprite;

        [SerializeField]
        private Sprite _shieldXSprite;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _reasonText;

        [SerializeField]
        private ResultRow[] _quotaRows;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private TMP_Text _retryLabel;

        [SerializeField]
        private Button _homeButton;

        [SerializeField]
        private TMP_Text _homeLabel;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;

        public void Init(in MessagePublisher.Publisher<LevelCommandScope> commands)
        {
            _commands = commands;

            _retryLabel.text = UiText.BUTTON_REPLAY;
            _homeLabel.text = UiText.BUTTON_HOME;

            _retryButton.onClick.AddListener(OnRetryClicked);
            _homeButton.onClick.AddListener(OnHomeClicked);
        }

        public void Apply(in LevelResult result, in LevelSnapshot snapshot)
        {
            var visual = ResultFailureVisuals.Get(result.Outcome);

            _tile.color = visual.Tile;
            _icon.sprite = visual.Icon == ResultFailureIcon.Clock ? _clockSprite : _shieldXSprite;
            _icon.color = visual.IconTint;
            _titleText.text = visual.Title;
            _reasonText.text = visual.Reason;

            ShowQuotas(in snapshot);
        }

        private void ShowQuotas(in LevelSnapshot snapshot)
        {
            var rowIndex = 0;
            var quotaCount = snapshot.QuotaCount;

            for (var i = 0; i < quotaCount && rowIndex < _quotaRows.Length; i++)
            {
                var quota = snapshot.GetQuota(i);

                if (quota.IsBonus)
                {
                    continue;
                }

                var row = _quotaRows[rowIndex++];

                row.gameObject.SetActive(true);
                row.Apply(ResultPopupFormat.CreateQuotaRow(in quota));
            }

            for (var i = rowIndex; i < _quotaRows.Length; i++)
            {
                _quotaRows[i].gameObject.SetActive(false);
            }
        }

        private void OnRetryClicked()
        {
            UiAudio.Tap();
            RetryRequestedMsg.Publish(in _commands, new RetryRequestedMsg());
        }

        private void OnHomeClicked()
        {
            UiAudio.Tap();
            QuitRequestedMsg.Publish(in _commands, new QuitRequestedMsg());
        }
    }
}
