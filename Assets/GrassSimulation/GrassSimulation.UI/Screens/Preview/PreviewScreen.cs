using EncosyTower.PageFlows.MonoPages;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class PreviewScreen : MonoPageBase<GrassPageFlowScopes>
    {
        private const float POLL_INTERVAL = 0.1f;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _goalsText;

        [SerializeField]
        private TMP_Text _timerText;

        [SerializeField]
        private PreviewQuotaRow[] _mainRows;

        [SerializeField]
        private GameObject _bonusDivider;

        [SerializeField]
        private PreviewQuotaRow _bonusRow;

        [SerializeField]
        private PlantIconSet _plantIcons;

        [SerializeField]
        private PreviewStarTile _goalTile;

        [SerializeField]
        private PreviewStarTile _timeTile;

        [SerializeField]
        private PreviewStarTile _cleanTile;

        [SerializeField]
        private Button _startButton;

        [SerializeField]
        private TMP_Text _startLabel;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private Processor.Hub<GameplayScope> _hub;
        private ProcessingContext _processingContext;
        private LevelId _appliedLevel;
        private float _pollTimer;
        private bool _hasApplied;

        private void Awake()
        {
            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();
            _hub = GlobalProcessor.Instance.Scope<GameplayScope>();
            _processingContext = ProcessingContext.DropIfNoHandler(warnNoHandler: false);

            _goalsText.text = UiText.PREVIEW_GOALS;
            _startLabel.text = UiText.BUTTON_START;

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
            QuitRequestedMsg.Publish(in _commands, new QuitRequestedMsg());
        }

        private void OnStartClicked()
        {
            StartRequestedMsg.Publish(in _commands, new StartRequestedMsg());
        }

        private void Refresh()
        {
            var result = GetLevelSnapshotRequest.TryProcess(in _hub, new GetLevelSnapshotRequest(), _processingContext);

            if (result.TryGetValue(out var snapshot) == false || (_hasApplied && snapshot.Level == _appliedLevel))
            {
                return;
            }

            _hasApplied = true;
            _appliedLevel = snapshot.Level;

            _titleText.text = PreviewScreenFormat.FormatTitle(snapshot.LevelIndex);
            _timerText.text = PreviewScreenFormat.FormatTimer(snapshot.TimeLimit);

            var bonus = ResultPopupFormat.GetBonus(in snapshot);

            ShowQuotas(in snapshot, in bonus);
            ShowStarRules(in snapshot, in bonus);
        }

        private void ShowQuotas(in LevelSnapshot snapshot, in ResultBonus bonus)
        {
            var rowIndex = 0;
            var quotaCount = snapshot.QuotaCount;

            for (var i = 0; i < quotaCount && rowIndex < _mainRows.Length; i++)
            {
                var quota = snapshot.GetQuota(i);

                if (quota.IsBonus)
                {
                    continue;
                }

                var row = _mainRows[rowIndex++];

                row.gameObject.SetActive(true);
                row.Apply(in quota, _plantIcons.Get(quota.Kind));
            }

            for (var i = rowIndex; i < _mainRows.Length; i++)
            {
                _mainRows[i].gameObject.SetActive(false);
            }

            var hasBonus = bonus.First.TryGetValue(out var bonusQuota);

            _bonusDivider.SetActive(hasBonus);
            _bonusRow.gameObject.SetActive(hasBonus);

            if (hasBonus)
            {
                _bonusRow.Apply(in bonusQuota, _plantIcons.Get(bonusQuota.Kind));
            }
        }

        private void ShowStarRules(in LevelSnapshot snapshot, in ResultBonus bonus)
        {
            _goalTile.Apply(star: 1, rule: UiText.STAR_RULE_GOAL);
            _timeTile.Apply(star: 2, rule: PreviewScreenFormat.FormatTimeRule(snapshot.TimeLimit));
            _cleanTile.Apply(star: 3, rule: PreviewScreenFormat.FormatCleanRule(bonus.First.HasValue));
        }
    }
}
