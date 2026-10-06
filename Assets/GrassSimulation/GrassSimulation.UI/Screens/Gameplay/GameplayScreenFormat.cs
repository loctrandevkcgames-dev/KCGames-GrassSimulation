using System;
using EncosyTower.Common;
using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class GameplayScreenFormat
    {
        public const float TIMER_WARNING_SECONDS = LevelSession.TIMER_WARNING_SECONDS;

        private const int SECONDS_PER_MINUTE = 60;
        private const float PERCENT_SCALE = 100f;

        public static bool IsTimerWarning(float remaining)
        {
            return remaining <= TIMER_WARNING_SECONDS;
        }

        public static string FormatTimer(float remaining)
        {
            var totalSeconds = (int)MathF.Ceiling(MathF.Max(remaining, 0f));
            var minutes = totalSeconds / SECONDS_PER_MINUTE;
            var seconds = totalSeconds % SECONDS_PER_MINUTE;

            return $"{minutes}:{seconds:00}";
        }

        public static string FormatQuotaProgress(int progress, int amount)
        {
            return string.Format(UiText.QUOTA_PROGRESS, Math.Min(progress, amount), amount);
        }

        public static float GetQuotaFraction(int progress, int amount)
        {
            return amount <= 0 ? 1f : Math.Clamp(value: (float)progress / amount, min: 0f, max: 1f);
        }

        public static string FormatBonus(PlantKind kind, int progress, int amount)
        {
            var label = PlantVisuals.GetLabel(kind).ToLowerInvariant();

            return string.Format(UiText.BONUS, label, Math.Min(progress, amount), amount);
        }

        public static string FormatTier(int tier)
        {
            return string.Format(UiText.TIER, tier);
        }

        public static string FormatXp(int xp, Option<int> nextThresholdXp)
        {
            return nextThresholdXp.TryGetValue(out var next)
                ? string.Format(UiText.XP, xp, next)
                : string.Format(UiText.XP_MAX, xp);
        }

        public static float GetXpFraction(int xp, int tierFloorXp, Option<int> nextThresholdXp)
        {
            if (nextThresholdXp.TryGetValue(out var next) == false)
            {
                return 1f;
            }

            var span = next - tierFloorXp;

            return span <= 0 ? 1f : Math.Clamp(value: (float)(xp - tierFloorXp) / span, min: 0f, max: 1f);
        }

        public static int GetClearedPercent(float fraction)
        {
            return (int)MathF.Floor(Math.Clamp(value: fraction, min: 0f, max: 1f) * PERCENT_SCALE);
        }

        public static string FormatCleared(int percent)
        {
            return string.Format(UiText.CLEARED, percent);
        }

        public static string FormatHitsLeft(int hits, int hitLimit)
        {
            return string.Format(UiText.HITS_LEFT, Math.Max(hitLimit - hits, 0));
        }
    }
}
