using GrassSimulation.Progression;
using UnityEngine;

namespace GrassSimulation.UI
{
    public sealed class MainMenuGardenGrid : MonoBehaviour
    {
        [SerializeField]
        private MainMenuGardenTile[] _tiles;

        [SerializeField]
        private Sprite _completedIcon;

        [SerializeField]
        private Sprite _nextIcon;

        [SerializeField]
        private Sprite _lockedIcon;

        public void Apply(in ProgressSnapshot progress)
        {
            var levelCount = progress.LevelCount;
            var completedCount = progress.CompletedCount;
            var nextLevelIndex = progress.NextLevelIndex;
            var first = GardenGridLayout.GetFirstLevelIndex(levelCount, nextLevelIndex, _tiles.Length);

            for (var i = 0; i < _tiles.Length; i++)
            {
                var state = GardenGridLayout.GetTileState(first + i, levelCount, completedCount, nextLevelIndex);

                _tiles[i].Apply(state, GetIcon(state));
            }
        }

        private Sprite GetIcon(GardenTileState state)
        {
            return state switch {
                GardenTileState.Completed => _completedIcon,
                GardenTileState.Next => _nextIcon,
                GardenTileState.Locked => _lockedIcon,
                _ => null,
            };
        }
    }
}
