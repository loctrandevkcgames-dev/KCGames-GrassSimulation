using System;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;

namespace GrassSimulation.UI
{
    public static class MainMenuScreenFormat
    {
        public static string FormatCoins(int coins)
        {
            return string.Format(UiText.COIN_AMOUNT, coins);
        }

        public static int GetMaxStars(int levelCount)
        {
            return levelCount * RewardRules.MAX_STARS;
        }

        public static string FormatProgress(int completedCount, int levelCount, int totalStars)
        {
            return string.Format(
                  UiText.GARDEN_PROGRESS
                , completedCount
                , levelCount
                , totalStars
                , GetMaxStars(levelCount)
            );
        }

        public static float GetProgressFraction(int completedCount, int levelCount)
        {
            return levelCount <= 0 ? 0f : Math.Clamp(value: (float)completedCount / levelCount, min: 0f, max: 1f);
        }

        public static bool IsGardenComplete(int completedCount, int levelCount)
        {
            return levelCount > 0 && completedCount >= levelCount;
        }

        public static string FormatNextHeader(int completedCount, int levelCount)
        {
            return IsGardenComplete(completedCount, levelCount) ? UiText.LAST_LEVEL_HEADER : UiText.NEXT_LEVEL_HEADER;
        }

        public static string FormatLevelNumber(int levelIndex)
        {
            return string.Format(UiText.LEVEL_NUMBER, levelIndex + 1);
        }

        public static string FormatQuotaChip(PlantKind kind, int amount)
        {
            return string.Format(UiText.QUOTA_CHIP, amount, PlantVisuals.GetLabel(kind).ToLowerInvariant());
        }
    }
}
