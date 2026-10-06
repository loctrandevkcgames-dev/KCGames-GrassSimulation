using EncosyTower.PageFlows.MonoPages;
using EncosyTower.PubSub;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class SettingsPopup : MonoPageBase<GrassPageFlowScopes>
    {
        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private Button _closeButton;

        [SerializeField]
        private TMP_Text _closeLabel;

        private MessagePublisher.Publisher<UiScope> _ui;

        private void Awake()
        {
            _ui = GlobalMessenger.Publisher.Scope<UiScope>();

            _titleText.text = UiText.SETTINGS;
            _closeLabel.text = UiText.BUTTON_CLOSE;

            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked()
        {
            UiAudio.Tap();
            SettingsRequestedMsg.Publish(in _ui, new SettingsRequestedMsg(IsOpen: false));
        }
    }
}
