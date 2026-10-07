using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class LoadoutBoosterRow : MonoBehaviour
    {
        [SerializeField]
        private BoosterKind _kind;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _stockText;

        [SerializeField]
        private TMP_Text _equipText;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private bool _isEquipped;

        public BoosterKind Kind => _kind;

        public void Init(in MessagePublisher.Publisher<LevelCommandScope> commands)
        {
            _commands = commands;

            _nameText.text = GameplayBoosterFormat.GetName(_kind);

            _button.onClick.AddListener(OnClicked);
        }

        public void Apply(in BoosterSlot slot)
        {
            _isEquipped = slot.IsEquipped;
            _stockText.text = GameplayBoosterFormat.FormatStock(slot.Stock);
            _equipText.text = LoadoutScreenFormat.FormatEquip(slot.IsEquipped);
            _background.color = slot.IsEquipped ? UiPalette.Soft : UiPalette.White;
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            UiAudio.Tap();
            SetBoosterEquippedRequestedMsg.Publish(
                  in _commands
                , new SetBoosterEquippedRequestedMsg(_kind, IsEquipped: _isEquipped == false)
            );
        }
    }
}
