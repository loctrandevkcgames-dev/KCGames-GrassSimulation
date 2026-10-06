using TMPro;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class PreviewStarTile : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _titleText;

        [SerializeField]
        private TMP_Text _ruleText;

        public void Apply(int star, string rule)
        {
            _titleText.text = PreviewScreenFormat.FormatStarTitle(star);
            _ruleText.text = rule;
        }
    }
}
