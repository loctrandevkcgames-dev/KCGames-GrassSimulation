using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class MainMenuLockedButton : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private TMP_Text _soonLabel;

        public void Init(string label)
        {
            _label.text = label;
            _soonLabel.text = UiText.COMING_SOON;
            _button.interactable = false;
        }
    }
}
