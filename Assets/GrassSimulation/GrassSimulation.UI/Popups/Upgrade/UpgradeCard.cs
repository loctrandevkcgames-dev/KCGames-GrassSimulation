using System;
using GrassSimulation.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class UpgradeCard : MonoBehaviour
    {
        private static readonly UpgradeStatKinds[] s_statOrder = {
            UpgradeStatKinds.CutRadius,
            UpgradeStatKinds.CuttingPower,
            UpgradeStatKinds.Speed,
        };

        [SerializeField]
        private Button _button;

        [SerializeField]
        private Image _iconTile;

        [SerializeField]
        private Image _icon;

        [SerializeField]
        private TMP_Text _title;

        [SerializeField]
        private TMP_Text[] _statLines;

        [SerializeField]
        private TMP_Text _hint;

        [SerializeField]
        private Sprite _ringsSprite;

        [SerializeField]
        private Sprite _boltSprite;

        private Action<int> _picked;
        private int _option;

        public void Init(int option, Action<int> picked)
        {
            _option = option;
            _picked = picked;

            _button.onClick.AddListener(OnClicked);
        }

        public void Apply(string upgradeId, in MachineStats before, in MachineStats after)
        {
            var visual = UpgradeVisuals.Get(upgradeId);

            _title.text = visual.Title;
            _hint.text = visual.Hint;
            _hint.gameObject.SetActive(visual.HasHint);
            _iconTile.color = visual.Tile;
            _icon.sprite = visual.Icon == UpgradeIcon.Rings ? _ringsSprite : _boltSprite;
            _icon.color = visual.IconTint;
            _icon.rectTransform.sizeDelta = new Vector2(x: visual.IconSize, y: visual.IconSize);

            var kinds = UpgradePopupFormat.GetStatKinds(in visual, in before, in after);
            var lineIndex = 0;

            for (var i = 0; i < s_statOrder.Length && lineIndex < _statLines.Length; i++)
            {
                var kind = s_statOrder[i];

                if ((kinds & kind) == 0)
                {
                    continue;
                }

                var line = _statLines[lineIndex++];

                line.text = UpgradePopupFormat.FormatStatLine(kind, in before, in after);
                line.gameObject.SetActive(true);
            }

            for (var i = lineIndex; i < _statLines.Length; i++)
            {
                _statLines[i].gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            _picked?.Invoke(_option);
        }
    }
}
