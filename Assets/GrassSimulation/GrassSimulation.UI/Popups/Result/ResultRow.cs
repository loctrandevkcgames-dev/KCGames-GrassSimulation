using TMPro;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class ResultRow : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private TMP_Text _value;

        [SerializeField]
        private GameObject _check;

        public void Apply(in ResultRowData data)
        {
            var hasValue = data.Value.TryGetValue(out var value);

            _label.text = data.Label;
            _label.color = data.Tone;
            _value.gameObject.SetActive(hasValue);
            _check.SetActive(data.ShowCheck);

            if (hasValue)
            {
                _value.text = value;
                _value.color = data.Tone;
            }
        }
    }
}
