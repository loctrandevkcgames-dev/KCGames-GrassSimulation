using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class PreviewScreenFormat
    {
        public static string FormatTitle(int levelIndex)
        {
            return MainMenuScreenFormat.FormatLevelNumber(levelIndex);
        }

        public static string FormatTimer(float timeLimit)
        {
            return GameplayScreenFormat.FormatTimer(timeLimit);
        }

        public static string FormatQuotaAmount(int amount)
        {
            return string.Format(UiText.QUOTA_AMOUNT, amount);
        }

        public static string FormatQuotaLabel(PlantKind kind, bool isBonus)
        {
            var label = PlantVisuals.GetLabel(kind);

            return isBonus ? string.Format(UiText.PREVIEW_BONUS, label) : label;
        }

        public static string FormatStarTitle(int star)
        {
            return string.Format(UiText.STAR_TITLE, star);
        }

        public static string FormatTimeRule(float timeLimit)
        {
            return string.Format(UiText.STAR_RULE_TIME, ResultPopupFormat.FormatThreshold(timeLimit));
        }

        public static string FormatCleanRule(bool hasBonus)
        {
            return hasBonus ? UiText.STAR_RULE_CLEAN_BONUS : UiText.STAR_RULE_CLEAN;
        }
    }
}
