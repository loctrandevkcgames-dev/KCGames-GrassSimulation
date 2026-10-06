using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class PauseSwitch : MonoBehaviour
    {
        private const float KNOB_INSET = 3f;

        [SerializeField]
        private PauseOption _option;

        [SerializeField]
        private Button _button;

        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private Image _track;

        [SerializeField]
        private RectTransform _knob;

        private bool _isOn;

        private void Awake()
        {
            _label.text = PauseOptions.GetLabel(_option);

            _button.onClick.AddListener(OnClicked);
        }

        private void OnEnable()
        {
            _isOn = PauseOptions.Read(_option);

            Show();
        }

        private void OnClicked()
        {
            _isOn = _isOn == false;

            PauseOptions.Write(_option, _isOn);
            Show();
        }

        private void Show()
        {
            var edge = _isOn ? 1f : 0f;
            var inset = _isOn ? -KNOB_INSET : KNOB_INSET;

            _track.color = _isOn ? UiPalette.Primary : UiPalette.SwitchOff;
            _knob.anchorMin = new Vector2(x: edge, y: 0.5f);
            _knob.anchorMax = new Vector2(x: edge, y: 0.5f);
            _knob.pivot = new Vector2(x: edge, y: 0.5f);
            _knob.anchoredPosition = new Vector2(x: inset, y: 0f);
        }
    }
}
