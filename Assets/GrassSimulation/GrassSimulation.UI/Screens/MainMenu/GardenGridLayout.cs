using System;

namespace GrassSimulation.UI
{
    public static class GardenGridLayout
    {
        public static int GetFirstLevelIndex(int levelCount, int nextLevelIndex, int tileCount)
        {
            if (tileCount <= 0 || levelCount <= 0)
            {
                return 0;
            }

            var clamped = Math.Clamp(value: nextLevelIndex, min: 0, max: levelCount - 1);

            return clamped / tileCount * tileCount;
        }

        /// <remarks>
        /// Progression is linear (GDD) and the snapshot has no per-level flags, so a level completed out of order
        /// after the next level is shown as locked.
        /// </remarks>
        public static GardenTileState GetTileState(
              int levelIndex
            , int levelCount
            , int completedCount
            , int nextLevelIndex
        )
        {
            if (levelIndex < 0 || levelIndex >= levelCount)
            {
                return GardenTileState.Empty;
            }

            var next = Math.Clamp(value: nextLevelIndex, min: 0, max: levelCount - 1);

            if (MainMenuScreenFormat.IsGardenComplete(completedCount, levelCount) || levelIndex < next)
            {
                return GardenTileState.Completed;
            }

            return levelIndex == next ? GardenTileState.Next : GardenTileState.Locked;
        }
    }
}
