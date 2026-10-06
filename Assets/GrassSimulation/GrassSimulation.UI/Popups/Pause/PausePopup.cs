using EncosyTower.PageFlows.MonoPages;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class PausePopup : MonoPageBase<GrassPageFlowScopes>
    {
        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private Button _resumeButton;

        [SerializeField]
        private TMP_Text _resumeLabel;

        [SerializeField]
        private Button _retryButton;

        [SerializeField]
        private TMP_Text _retryLabel;

        [SerializeField]
        private Button _quitButton;

        [SerializeField]
        private TMP_Text _quitLabel;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;

        private void Awake()
        {
            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();

            _titleText.text = UiText.PAUSE;
            _resumeLabel.text = UiText.BUTTON_RESUME;
            _retryLabel.text = UiText.BUTTON_REPLAY;
            _quitLabel.text = UiText.BUTTON_QUIT;

            _resumeButton.onClick.AddListener(OnResumeClicked);
            _retryButton.onClick.AddListener(OnRetryClicked);
            _quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnResumeClicked()
        {
            UiAudio.Tap();
            PauseRequestedMsg.Publish(in _commands, new PauseRequestedMsg(Paused: false));
        }

        private void OnRetryClicked()
        {
            UiAudio.Tap();
            RetryRequestedMsg.Publish(in _commands, new RetryRequestedMsg());
        }

        private void OnQuitClicked()
        {
            UiAudio.Tap();
            QuitRequestedMsg.Publish(in _commands, new QuitRequestedMsg());
        }
    }
}
