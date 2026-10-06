using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class PreviewQuotaRow : MonoBehaviour
    {
        [SerializeField]
        private Image _iconBox;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private TMP_Text _amount;

        [SerializeField]
        private Sprite _bonusIcon;

        public void Apply(in QuotaSnapshot quota, Sprite kindIcon)
        {
            _label.text = PreviewScreenFormat.FormatQuotaLabel(quota.Kind, quota.IsBonus);
            _amount.text = PreviewScreenFormat.FormatQuotaAmount(quota.Amount);

            if (quota.IsBonus)
            {
                _iconBox.color = UiPalette.BonusBox;
                _icon.sprite = _bonusIcon;
                _icon.color = UiPalette.Sun;
                _label.color = UiPalette.Muted;
                _amount.color = UiPalette.Muted;

                return;
            }

            var colors = PlantVisuals.GetColors(quota.Kind);

            _iconBox.color = colors.Box;
            _icon.sprite = kindIcon;
            _icon.color = colors.Icon;
            _label.color = UiPalette.Ink;
            _amount.color = UiPalette.Ink;
        }
    }
}
