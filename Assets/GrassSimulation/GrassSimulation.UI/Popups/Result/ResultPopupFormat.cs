using System;
using EncosyTower.Common;
using EncosyTower.Pooling;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using UnityEngine;

namespace GrassSimulation.UI
{
    public static class ResultPopupFormat
    {
        private const char LINE_BREAK = '\n';
        private const int SECONDS_PER_MINUTE = 60;

        public static bool HasNextLevel(int levelIndex, int levelCount)
        {
            return levelIndex + 1 < levelCount;
        }

        public static float GetTimeThreshold(float timeLimit)
        {
            return timeLimit * LevelSession.SECOND_STAR_TIME_FRACTION;
        }

        public static bool HasTimeStar(float remaining, float timeLimit)
        {
            return remaining >= GetTimeThreshold(timeLimit);
        }

        public static string FormatClock(float seconds)
        {
            var totalSeconds = (int)MathF.Floor(MathF.Max(seconds, 0f));
            var minutes = totalSeconds / SECONDS_PER_MINUTE;

            return $"{minutes}:{totalSeconds % SECONDS_PER_MINUTE:00}";
        }

        public static string FormatThreshold(float timeLimit)
        {
            return FormatClock(MathF.Ceiling(GetTimeThreshold(timeLimit)));
        }

        public static string FormatTimeStar(float remaining, float timeLimit)
        {
            return string.Format(
                  UiText.RESULT_TIME
                , FormatClock(remaining)
                , FormatThreshold(timeLimit)
            );
        }

        public static ResultBonus GetBonus(in LevelSnapshot snapshot)
        {
            Option<QuotaSnapshot> first = Option.None;

            var areAllMet = true;
            var quotaCount = snapshot.QuotaCount;

            for (var i = 0; i < quotaCount; i++)
            {
                var quota = snapshot.GetQuota(i);

                if (quota.IsBonus == false)
                {
                    continue;
                }

                if (first.HasValue == false)
                {
                    first = quota;
                }

                areAllMet &= quota.IsMet;
            }

            return new ResultBonus(first, areAllMet);
        }

        public static bool HasCleanStar(int protectedHits, in ResultBonus bonus)
        {
            return protectedHits == 0 && bonus.AreAllMet;
        }

        public static string FormatCleanStar(in ResultBonus bonus)
        {
            if (bonus.First.TryGetValue(out var quota) == false)
            {
                return UiText.RESULT_CLEAN;
            }

            var label = PlantVisuals.GetLabel(quota.Kind).ToLowerInvariant();

            return string.Format(UiText.RESULT_CLEAN_BONUS, label, quota.Amount);
        }

        public static ResultRowData CreateGoalRow()
        {
            return CreateMetRow(UiText.RESULT_GOAL, Option.None, UiPalette.Ink);
        }

        public static ResultRowData CreateTimeRow(float remaining, float timeLimit)
        {
            var label = FormatTimeStar(remaining, timeLimit);

            return HasTimeStar(remaining, timeLimit)
                ? CreateMetRow(label, Option.None, UiPalette.Ink)
                : CreateUnmetRow(label, Option.None, UiPalette.Muted);
        }

        public static ResultRowData CreateCleanRow(int protectedHits, in ResultBonus bonus)
        {
            var label = FormatCleanStar(in bonus);

            if (HasCleanStar(protectedHits, in bonus))
            {
                return CreateMetRow(label, Option.None, UiPalette.Ink);
            }

            Option<string> progress = bonus.First.TryGetValue(out var quota)
                ? GameplayScreenFormat.FormatQuotaProgress(quota.Progress, quota.Amount)
                : Option.None;

            return CreateUnmetRow(label, progress, UiPalette.Muted);
        }

        public static ResultRowData CreateQuotaRow(in QuotaSnapshot quota)
        {
            var label = PlantVisuals.GetLabel(quota.Kind);
            var progress = GameplayScreenFormat.FormatQuotaProgress(quota.Progress, quota.Amount);

            if (quota.IsMet)
            {
                return CreateMetRow(label, progress, UiPalette.Primary);
            }

            var current = Math.Min(quota.Progress, quota.Amount);
            var shortfall = string.Format(UiText.QUOTA_SHORT, current, quota.Amount, quota.Amount - current);

            return CreateUnmetRow(label, shortfall, UiPalette.Ink);
        }

        public static string FormatCoinBreakdown(in LevelSettlement settlement)
        {
            if (settlement.CoinsGranted <= 0)
            {
                return UiText.COINS_NONE;
            }

            using var _ = StringBuilderPool.Rent(out var builder);

            if (settlement.FirstWinCoins > 0)
            {
                builder.AppendFormat(UiText.COINS_FIRST_WIN, settlement.FirstWinCoins);
            }

            if (settlement.NewStars > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append(LINE_BREAK);
                }

                var starCoins = settlement.CoinsGranted - settlement.FirstWinCoins;

                builder.AppendFormat(UiText.COINS_NEW_STARS, settlement.NewStars, starCoins);
            }

            return builder.ToString();
        }

        public static string FormatCoinTotal(int coins)
        {
            return string.Format(UiText.COINS_TOTAL, coins);
        }

        private static ResultRowData CreateMetRow(string label, Option<string> value, Color tone)
        {
            return new ResultRowData(Label: label, Value: value, Tone: tone, ShowCheck: true);
        }

        private static ResultRowData CreateUnmetRow(string label, Option<string> value, Color tone)
        {
            return new ResultRowData(Label: label, Value: value, Tone: tone, ShowCheck: false);
        }
    }
}
