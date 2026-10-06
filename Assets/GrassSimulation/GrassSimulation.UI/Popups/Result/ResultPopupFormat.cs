using System;
using EncosyTower.Common;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    public static class ResultPopupFormat
    {
        private const int SECONDS_PER_MINUTE = 60;

        public static bool HasNextLevel(int levelIndex, int levelCount)
        {
            return levelIndex + 1 < levelCount;
        }

        public static float GetTimeThreshold(float timeLimit, float star2TimeLeft)
        {
            return timeLimit * star2TimeLeft;
        }

        public static bool HasTimeStar(float remaining, float timeLimit, float star2TimeLeft)
        {
            return remaining >= GetTimeThreshold(timeLimit, star2TimeLeft);
        }

        public static string FormatClock(float seconds)
        {
            var totalSeconds = (int)MathF.Floor(MathF.Max(seconds, 0f));
            var minutes = totalSeconds / SECONDS_PER_MINUTE;

            return $"{minutes}:{totalSeconds % SECONDS_PER_MINUTE:00}";
        }

        public static string FormatThreshold(float timeLimit, float star2TimeLeft)
        {
            return FormatClock(MathF.Ceiling(GetTimeThreshold(timeLimit, star2TimeLeft)));
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

        public static bool HasStar(StarFlags stars, StarFlags star)
        {
            return (stars & star) == star;
        }

        public static string FormatCleanStar(in LevelSnapshot snapshot, float remaining)
        {
            if (snapshot.IsTimed == false)
            {
                return UiText.RESULT_CLEAN;
            }

            return string.Format(
                  UiText.RESULT_CLEAN_TIME
                , FormatClock(remaining)
                , FormatThreshold(snapshot.TimeLimit, snapshot.Star2TimeLeft)
            );
        }

        public static string FormatSideStar(in ResultBonus bonus, float clearedFraction)
        {
            if (bonus.First.TryGetValue(out var quota))
            {
                var label = PlantVisuals.GetLabel(quota.Kind).ToLowerInvariant();

                return string.Format(UiText.RESULT_SIDE_QUOTA, label, quota.Amount);
            }

            return string.Format(
                  UiText.RESULT_SIDE_SWEEP
                , GameplayScreenFormat.GetClearedPercent(clearedFraction)
                , GameplayScreenFormat.GetClearedPercent(StarRules.SIDE_SWEEP_FRACTION)
            );
        }

        public static ResultRowData CreateGoalRow()
        {
            return CreateMetRow(UiText.RESULT_GOAL, Option.None, UiPalette.Ink);
        }

        public static ResultRowData CreateCleanRow(in LevelResult result, in LevelSnapshot snapshot)
        {
            var label = FormatCleanStar(in snapshot, result.RemainingTime);

            return HasStar(result.Stars, StarFlags.Clean)
                ? CreateMetRow(label, Option.None, UiPalette.Ink)
                : CreateUnmetRow(label, Option.None, UiPalette.Muted);
        }

        public static ResultRowData CreateSideRow(
              in LevelResult result
            , in LevelSnapshot snapshot
            , in ResultBonus bonus
        )
        {
            var label = FormatSideStar(in bonus, snapshot.ClearedFraction);

            if (HasStar(result.Stars, StarFlags.Side))
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
