using System;
using System.Globalization;
using System.Text;
using EncosyTower.Pooling;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    public static class UpgradePopupFormat
    {
        private const float STAT_EPSILON = 1e-4f;

        private static readonly string s_afterColor = ColorUtility.ToHtmlStringRGB(UiPalette.Primary);

        public static string FormatLevelUp(int tier)
        {
            return string.Format(UiText.LEVEL_UP, tier);
        }

        public static bool TryFormatUnlocked(PlantKindMask kinds, out string text)
        {
            var count = kinds.Count;

            if (count == 0)
            {
                text = null;
                return false;
            }

            using var _ = StringBuilderPool.Rent(out var builder);

            var written = 0;

            for (var i = 1; i < PlantKindExtensions.Length; i++)
            {
                var kind = (PlantKind)i;

                if (kinds.Contains(kind) == false)
                {
                    continue;
                }

                if (written > 0)
                {
                    builder.Append(written == count - 1 ? UiText.LIST_AND : UiText.LIST_COMMA);
                }

                builder.Append(PlantVisuals.GetLabel(kind).ToLowerInvariant());
                written++;
            }

            text = string.Format(UiText.UNLOCKED, builder.ToString());
            return true;
        }

        public static string FormatMeters(float value)
        {
            return string.Concat(FormatNumber(value), UiText.UNIT_METER);
        }

        public static string FormatSpeed(float value)
        {
            return string.Concat(FormatNumber(value), UiText.UNIT_SPEED);
        }

        public static string FormatNumber(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static UpgradeStatKinds GetStatKinds(
              in UpgradeVisual visual
            , in MachineStats before
            , in MachineStats after
        )
        {
            var kinds = visual.Stats;

            if (visual.ShowUnchangedStats)
            {
                return kinds;
            }

            if (IsChanged(before.CutRadius, after.CutRadius) == false)
            {
                kinds &= ~UpgradeStatKinds.CutRadius;
            }

            if (IsChanged(before.CuttingPower, after.CuttingPower) == false)
            {
                kinds &= ~UpgradeStatKinds.CuttingPower;
            }

            if (IsChanged(before.Speed, after.Speed) == false)
            {
                kinds &= ~UpgradeStatKinds.Speed;
            }

            return kinds;
        }

        public static string FormatStatLine(UpgradeStatKinds kind, in MachineStats before, in MachineStats after)
        {
            return kind switch {
                UpgradeStatKinds.CutRadius => FormatLine(
                      UiText.STAT_CUT_RADIUS
                    , FormatMeters(before.CutRadius)
                    , FormatMeters(after.CutRadius)
                ),
                UpgradeStatKinds.CuttingPower => FormatLine(
                      UiText.STAT_CUTTING_POWER
                    , FormatNumber(before.CuttingPower)
                    , FormatNumber(after.CuttingPower)
                ),
                UpgradeStatKinds.Speed => FormatLine(
                      UiText.STAT_SPEED
                    , FormatNumber(before.Speed)
                    , FormatSpeed(after.Speed)
                ),
                _ => string.Empty,
            };
        }

        private static string FormatLine(string label, string beforeText, string afterText)
        {
            return string.Format(UiText.STAT_LINE, label, beforeText, s_afterColor, afterText);
        }

        private static bool IsChanged(float before, float after)
        {
            return MathF.Abs(after - before) > STAT_EPSILON;
        }
    }
}
