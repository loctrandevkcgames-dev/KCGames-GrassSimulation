using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class MainMenuQuotaChip : MonoBehaviour
    {
        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _label;

        public void Apply(in QuotaSettings quota, Sprite kindIcon)
        {
            var colors = PlantVisuals.GetColors(quota.Kind);

            _background.color = colors.Box;
            _icon.sprite = kindIcon;
            _icon.color = colors.Icon;
            _label.color = colors.Icon;
            _label.text = MainMenuScreenFormat.FormatQuotaChip(quota.Kind, quota.Amount);
        }
    }
}
