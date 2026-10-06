using System;
using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class MainMenuScreenFormat
    {
        public static int GetMaxStars(int levelCount)
        {
            return levelCount * StarRules.MAX_STARS;
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

        public static string FormatTimer(bool isTimed, float timeLimit)
        {
            return isTimed ? GameplayScreenFormat.FormatTimer(timeLimit) : UiText.TIMER_UNLIMITED;
        }

        public static string FormatQuotaChip(PlantKind kind, int amount)
        {
            return string.Format(UiText.QUOTA_CHIP, amount, PlantVisuals.GetLabel(kind).ToLowerInvariant());
        }
    }
}
