using UnityEngine;

namespace GrassSimulation.UI
{
    public static class GardenTileVisuals
    {
        private static readonly Color s_lockedIcon = new(r: 1f, g: 1f, b: 1f, a: 0.8f);

        public static Color GetBackground(GardenTileState state)
        {
            return state switch {
                GardenTileState.Completed => UiPalette.GardenMowed,
                GardenTileState.Next => UiPalette.Sun,
                GardenTileState.Locked => UiPalette.GardenLocked,
                _ => Color.clear,
            };
        }

        public static Color GetIconColor(GardenTileState state)
        {
            return state switch {
                GardenTileState.Completed => UiPalette.Primary,
                GardenTileState.Next => UiPalette.White,
                GardenTileState.Locked => s_lockedIcon,
                _ => Color.clear,
            };
        }
    }
}
