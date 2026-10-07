using EncosyTower.PageFlows.MonoPages;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class LoadoutScreen : MonoPageBase<GrassPageFlowScopes>
    {
        private const float POLL_INTERVAL = 0.1f;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private LoadoutMachineCard[] _cards;

        [SerializeField]
        private TMP_Text _boosterTitleText;

        [SerializeField]
        private TMP_Text _boosterEmptyText;

        [SerializeField]
        private Button _startButton;

        [SerializeField]
        private TMP_Text _startLabel;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private Processor.Hub<GameplayScope> _hub;
        private ProcessingContext _processingContext;
        private LoadoutSnapshot _applied;
        private float _pollTimer;
        private bool _hasApplied;

        private void Awake()
        {
            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();
            _hub = GlobalProcessor.Instance.Scope<GameplayScope>();
            _processingContext = ProcessingContext.DropIfNoHandler(warnNoHandler: false);

            _titleText.text = UiText.LOADOUT_TITLE;
            _boosterTitleText.text = UiText.LOADOUT_BOOSTERS;
            _boosterEmptyText.text = UiText.LOADOUT_BOOSTERS_EMPTY;
            _startLabel.text = UiText.BUTTON_START;

            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].Init(in _commands);
            }

            _backButton.onClick.AddListener(OnBackClicked);
            _startButton.onClick.AddListener(OnStartClicked);
        }

        private void OnEnable()
        {
            _hasApplied = false;
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
            _backButton.onClick.RemoveListener(OnBackClicked);
            _startButton.onClick.RemoveListener(OnStartClicked);
        }

        private void OnBackClicked()
        {
            UiAudio.Tap();
            BackToPreviewRequestedMsg.Publish(in _commands, new BackToPreviewRequestedMsg());
        }

        private void OnStartClicked()
        {
            UiAudio.Tap();
            StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
        }

        private void Refresh()
        {
            var result = GetLoadoutRequest.TryProcess(in _hub, new GetLoadoutRequest(), _processingContext);

            if (result.TryGetValue(out var snapshot) == false || (_hasApplied && snapshot == _applied))
            {
                return;
            }

            _hasApplied = true;
            _applied = snapshot;

            var best = LoadoutScreenFormat.GetBestStats(in snapshot);

            for (var i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                var isShown = i < snapshot.MachineCount;

                card.gameObject.SetActive(isShown);

                if (isShown)
                {
                    var machine = snapshot.GetMachine(i);

                    card.Apply(in machine, in best, isSelected: machine.Id == snapshot.Selected);
                }
            }
        }
    }
}
