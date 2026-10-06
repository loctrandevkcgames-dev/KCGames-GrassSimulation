using UnityEngine;
using UnityEngine.UI;

namespace GrassSimulation.UI
{
    public sealed class MainMenuGardenTile : MonoBehaviour
    {
        [SerializeField]
        private Image _background;

        [SerializeField]
        private Image _icon;

        public void Apply(GardenTileState state, Sprite icon)
        {
            _background.enabled = state != GardenTileState.Empty;
            _background.color = GardenTileVisuals.GetBackground(state);

            _icon.enabled = icon != null;
            _icon.sprite = icon;
            _icon.color = GardenTileVisuals.GetIconColor(state);
        }
    }
}
