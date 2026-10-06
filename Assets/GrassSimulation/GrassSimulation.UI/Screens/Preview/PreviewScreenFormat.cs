using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class PreviewScreenFormat
    {
        public static string FormatTitle(int levelIndex)
        {
            return MainMenuScreenFormat.FormatLevelNumber(levelIndex);
        }

        public static string FormatTimer(bool isTimed, float timeLimit)
        {
            return MainMenuScreenFormat.FormatTimer(isTimed, timeLimit);
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

        public static string FormatCleanRule(bool isTimed, float timeLimit, float star2TimeLeft)
        {
            if (isTimed == false)
            {
                return UiText.STAR_RULE_CLEAN;
            }

            return string.Format(
                  UiText.STAR_RULE_CLEAN_TIME
                , ResultPopupFormat.FormatThreshold(timeLimit, star2TimeLeft)
            );
        }

        public static string FormatSideRule(bool hasBonus)
        {
            if (hasBonus)
            {
                return UiText.STAR_RULE_SIDE_QUOTA;
            }

            var percent = GameplayScreenFormat.GetClearedPercent(StarRules.SIDE_SWEEP_FRACTION);

            return string.Format(UiText.STAR_RULE_SIDE_SWEEP, percent);
        }
    }
}
