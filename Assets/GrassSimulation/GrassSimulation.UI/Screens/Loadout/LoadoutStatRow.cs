using TMPro;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class LoadoutStatRow : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private TMP_Text _value;

        [SerializeField]
        private RectTransform _fill;

        public void Apply(string label, string value, float fill)
        {
            _label.text = label;
            _value.text = value;
            _fill.anchorMax = new Vector2(fill, 1f);
        }
    }
}
