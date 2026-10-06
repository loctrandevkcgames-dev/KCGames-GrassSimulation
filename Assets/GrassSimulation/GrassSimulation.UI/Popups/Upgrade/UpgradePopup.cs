using System.Collections.Generic;
using EncosyTower.PageFlows.MonoPages;
using EncosyTower.Processing;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class UpgradePopup : MonoPageBase<GrassPageFlowScopes>
    {
        private const float POLL_INTERVAL = 0.1f;

        [SerializeField]
        private TMP_Text _pausedText;

        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private GameObject _unlockRow;

        [SerializeField]
        private TMP_Text _unlockText;

        [SerializeField]
        private TMP_Text _promptText;

        [SerializeField]
        private UpgradeCard[] _cards;

        private readonly List<ISubscription> _subscriptions = new();

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private Processor.Hub<GameplayScope> _hub;
        private ProcessingContext _processingContext;
        private UpgradeInputLock _inputLock;
        private float _pollTimer;

        private bool _hasChoice;
        private LevelSnapshot _appliedSnapshot;

        private void Awake()
        {
            _commands = GlobalMessenger.Publisher.Scope<LevelCommandScope>();
            _hub = GlobalProcessor.Instance.Scope<GameplayScope>();
            _processingContext = ProcessingContext.DropIfNoHandler(warnNoHandler: false);

            _pausedText.text = UiText.TIME_PAUSED;
            _promptText.text = UiText.CHOOSE_UPGRADE;

            for (var i = 0; i < _cards.Length; i++)
            {
                _cards[i].Init(i, OnCardPicked);
            }
        }

        private void OnEnable()
        {
            _hasChoice = false;
            _pollTimer = 0f;
            _inputLock.Start(Time.unscaledTime);

            var subscriber = GlobalMessenger.Subscriber.Scope<GameplayScope>();

            _subscriptions.Add(UpgradeChosenMsg.Subscribe(in subscriber, OnUpgradeChosen));

            Refresh();
        }

        private void OnDisable()
        {
            _subscriptions.Unsubscribe();
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

        private void OnUpgradeChosen(UpgradeChosenMsg message)
        {
            Refresh();
        }

        private void OnCardPicked(int option)
        {
            if (_inputLock.IsLocked(Time.unscaledTime))
            {
                return;
            }

            UpgradeRequestedMsg.Publish(in _commands, new UpgradeRequestedMsg(Option: option));

            _inputLock.Start(Time.unscaledTime);
        }

        private void Refresh()
        {
            var result = GetLevelSnapshotRequest.TryProcess(in _hub, new GetLevelSnapshotRequest(), _processingContext);

            if (result.TryGetValue(out var snapshot) == false || HasChoiceChanged(in snapshot) == false)
            {
                return;
            }

            var isNewChoice = _hasChoice && snapshot.UpgradeTier != _appliedSnapshot.UpgradeTier;

            _hasChoice = true;
            _appliedSnapshot = snapshot;

            if (isNewChoice)
            {
                _inputLock.Start(Time.unscaledTime);
            }

            Show(in snapshot);
        }

        private bool HasChoiceChanged(in LevelSnapshot snapshot)
        {
            return _hasChoice == false
                || snapshot.UpgradeTier != _appliedSnapshot.UpgradeTier
                || snapshot.PendingUpgrades != _appliedSnapshot.PendingUpgrades
                || snapshot.UnlockedKinds != _appliedSnapshot.UnlockedKinds
                || snapshot.Stats != _appliedSnapshot.Stats
                || snapshot.UpgradeOptionCount != _appliedSnapshot.UpgradeOptionCount
                || snapshot.Upgrade0Stats != _appliedSnapshot.Upgrade0Stats
                || snapshot.Upgrade1Stats != _appliedSnapshot.Upgrade1Stats
                || snapshot.Upgrade0Id != _appliedSnapshot.Upgrade0Id
                || snapshot.Upgrade1Id != _appliedSnapshot.Upgrade1Id;
        }

        private void Show(in LevelSnapshot snapshot)
        {
            _titleText.text = UpgradePopupFormat.FormatLevelUp(snapshot.UpgradeTier);

            var hasUnlock = UpgradePopupFormat.TryFormatUnlocked(snapshot.UnlockedKinds, out var unlocked);

            _unlockRow.SetActive(hasUnlock);

            if (hasUnlock)
            {
                _unlockText.text = unlocked;
            }

            var before = snapshot.Stats;

            for (var i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                var isAvailable = i < snapshot.UpgradeOptionCount;

                card.gameObject.SetActive(isAvailable);

                if (isAvailable)
                {
                    var after = snapshot.GetUpgradeStats(i);

                    card.Apply(snapshot.GetUpgradeId(i), in before, in after);
                }
            }
        }
    }
}
