using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class LoadoutMachineCard : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _badgeText;

        [SerializeField]
        private TMP_Text _tradeOffText;

        [SerializeField]
        private LoadoutStatRow _radiusRow;

        [SerializeField]
        private LoadoutStatRow _speedRow;

        [SerializeField]
        private LoadoutStatRow _powerRow;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private MachineId _machine;

        public void Init(in MessagePublisher.Publisher<LevelCommandScope> commands)
        {
            _commands = commands;

            _button.onClick.AddListener(OnClicked);
        }

        public void Apply(in LoadoutMachine machine, in MachineStats best, bool isSelected)
        {
            var stats = machine.Stats;

            _machine = machine.Id;
            _nameText.text = machine.Name;
            _tradeOffText.text = machine.TradeOff;
            _badgeText.text = UiText.LOADOUT_SELECTED;
            _badgeText.gameObject.SetActive(isSelected);
            _background.color = isSelected ? UiPalette.Soft : UiPalette.White;

            _radiusRow.Apply(
                  UiText.STAT_CUT_RADIUS
                , UpgradePopupFormat.FormatMeters(stats.CutRadius)
                , LoadoutScreenFormat.GetBarFill(stats.CutRadius, best.CutRadius)
            );

            _speedRow.Apply(
                  UiText.STAT_SPEED
                , UpgradePopupFormat.FormatSpeed(stats.Speed)
                , LoadoutScreenFormat.GetBarFill(stats.Speed, best.Speed)
            );

            _powerRow.Apply(
                  UiText.STAT_CUTTING_POWER
                , UpgradePopupFormat.FormatNumber(stats.CuttingPower)
                , LoadoutScreenFormat.GetBarFill(stats.CuttingPower, best.CuttingPower)
            );
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            UiAudio.Tap();
            SelectMachineRequestedMsg.Publish(in _commands, new SelectMachineRequestedMsg(_machine));
        }
    }
}
