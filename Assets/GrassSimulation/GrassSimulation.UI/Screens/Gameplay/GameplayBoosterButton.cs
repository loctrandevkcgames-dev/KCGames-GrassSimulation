using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class GameplayBoosterButton : MonoBehaviour
    {
        [SerializeField]
        private BoosterKind _kind;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _ring;

        [SerializeField]
        private TMP_Text _nameText;

        [SerializeField]
        private TMP_Text _glyphText;

        [SerializeField]
        private TMP_Text _countText;

        private MessagePublisher.Publisher<LevelCommandScope> _commands;
        private BoosterSlot _applied;
        private string _appliedGlyph;
        private bool _hasApplied;

        public BoosterKind Kind => _kind;

        public void Init(in MessagePublisher.Publisher<LevelCommandScope> commands)
        {
            _commands = commands;

            _nameText.text = GameplayBoosterFormat.GetName(_kind);

            _button.onClick.AddListener(OnClicked);
        }

        public void ResetCache()
        {
            _hasApplied = false;
        }

        public void Apply(in BoosterSlot slot)
        {
            var isShown = GameplayBoosterFormat.IsShown(slot.State);

            if (gameObject.activeSelf != isShown)
            {
                gameObject.SetActive(isShown);
            }

            if (isShown == false)
            {
                _hasApplied = false;
                return;
            }

            var glyph = GameplayBoosterFormat.GetGlyph(in slot);

            if (_hasApplied && _applied == slot && glyph == _appliedGlyph)
            {
                return;
            }

            var styleChanged = _hasApplied == false || _applied.State != slot.State;

            _applied = slot;
            _appliedGlyph = glyph;
            _hasApplied = true;

            _glyphText.text = glyph;
            _countText.text = GameplayBoosterFormat.FormatStock(slot.Stock);
            _ring.fillAmount = GameplayBoosterFormat.GetRingFill(in slot);

            if (styleChanged)
            {
                ApplyStyle(slot.State);
            }
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void ApplyStyle(BoosterSlotState state)
        {
            var ink = GameplayBoosterFormat.GetInk(state);

            _background.color = GameplayBoosterFormat.GetBackground(state);
            _ring.gameObject.SetActive(state == BoosterSlotState.Running);
            _button.interactable = GameplayBoosterFormat.IsTappable(state);
            _nameText.color = ink;
            _glyphText.color = ink;
            _countText.color = ink;
        }

        private void OnClicked()
        {
            UiAudio.Tap();
            ActivateBoosterRequestedMsg.Publish(in _commands, new ActivateBoosterRequestedMsg(_kind));
        }
    }
}
