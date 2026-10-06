using EncosyTower.PubSub;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class VolumeSlider : MonoBehaviour, IPointerUpHandler
    {
        [SerializeField]
        private VolumeOption _option;

        [SerializeField]
        private Slider _slider;

        [SerializeField]
        private TMP_Text _label;

        private VolumePublishThrottle _throttle;

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_throttle.Flush())
            {
                Publish();
            }

            if (_option == VolumeOption.Sfx)
            {
                UiAudio.Tap();
            }
        }

        private void Awake()
        {
            _label.text = VolumeOptions.GetLabel(_option);

            _slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnEnable()
        {
            _throttle = default;

            _slider.SetValueWithoutNotify(VolumeOptions.Read(_option));
        }

        private void Update()
        {
            if (_throttle.Tick(Time.unscaledDeltaTime))
            {
                Publish();
            }
        }

        private void OnDisable()
        {
            if (_throttle.Flush())
            {
                Publish();
            }
        }

        private void OnDestroy()
        {
            _slider.onValueChanged.RemoveListener(OnValueChanged);
        }

        private void OnValueChanged(float value)
        {
            VolumeOptions.Write(_option, value);

            _throttle.MarkChanged();
        }

        private static void Publish()
        {
            var publisher = GlobalMessenger.Publisher.Scope<GameplayScope>();

            PlayerOptionsChangedMsg.Publish(in publisher, new PlayerOptionsChangedMsg());
        }
    }
}
