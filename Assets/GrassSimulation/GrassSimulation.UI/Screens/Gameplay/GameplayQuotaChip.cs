using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class GameplayQuotaChip : MonoBehaviour
    {
        [SerializeField]
        private Image _iconBox;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private TMP_Text _count;

        [SerializeField]
        private Image _barTrack;

        [SerializeField]
        private RectTransform _barFill;

        [SerializeField]
        private Image _barFillImage;

        [SerializeField]
        private Sprite _checkIcon;

        private QuotaSnapshot _applied;
        private bool _hasApplied;

        public void ResetCache()
        {
            _hasApplied = false;
        }

        public void Apply(in QuotaSnapshot quota, Sprite kindIcon)
        {
            if (_hasApplied && _applied == quota)
            {
                return;
            }

            var kindChanged = _hasApplied == false || _applied.Kind != quota.Kind;
            var metChanged = _hasApplied == false || _applied.IsMet != quota.IsMet;

            _applied = quota;
            _hasApplied = true;

            if (kindChanged)
            {
                _label.text = PlantVisuals.GetLabel(quota.Kind);
            }

            _count.text = quota.IsMet
                ? UiText.QUOTA_DONE
                : GameplayScreenFormat.FormatQuotaRemaining(quota.Progress, quota.Amount);

            if (kindChanged || metChanged)
            {
                ApplyStyle(quota, kindIcon);
            }

            var fraction = GameplayScreenFormat.GetQuotaFraction(quota.Progress, quota.Amount);

            _barFill.anchorMax = new Vector2(x: fraction, y: 1f);
        }

        private void ApplyStyle(in QuotaSnapshot quota, Sprite kindIcon)
        {
            if (quota.IsMet)
            {
                _iconBox.color = UiPalette.Primary;
                _icon.sprite = _checkIcon;
                _icon.color = UiPalette.White;
                _label.color = UiPalette.Primary;
                _count.color = UiPalette.Primary;
                _barTrack.gameObject.SetActive(false);

                return;
            }

            var colors = PlantVisuals.GetColors(quota.Kind);

            _iconBox.color = colors.Box;
            _icon.sprite = kindIcon;
            _icon.color = colors.Icon;
            _label.color = UiPalette.Ink;
            _count.color = UiPalette.Ink;
            _barTrack.color = colors.BarTrack;
            _barFillImage.color = colors.BarFill;
            _barTrack.gameObject.SetActive(true);
        }
    }
}
