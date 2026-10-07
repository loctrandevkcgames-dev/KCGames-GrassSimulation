using System;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    public static class GameplayBoosterFormat
    {
        public const string GLYPH_READY = "▶";
        public const string GLYPH_BLOCKED = "×";
        public const string GLYPH_USED = "—";

        public static string GetName(BoosterKind kind)
        {
            return kind switch {
                BoosterKind.Turbo => UiText.BOOSTER_TURBO,
                _ => UiText.BOOSTER_EXTRA_TIME,
            };
        }

        public static string FormatStock(int stock)
        {
            return string.Format(UiText.BOOSTER_STOCK, Math.Max(stock, 0));
        }

        public static string FormatRunning(float remaining)
        {
            return string.Format(UiText.BOOSTER_RUNNING, Mathf.CeilToInt(Mathf.Max(remaining, 0f)));
        }

        public static string FormatExtraTime(float seconds)
        {
            return string.Format(UiText.BOOSTER_EXTRA_TIME_FLOAT, Mathf.RoundToInt(seconds));
        }

        public static string GetGlyph(in BoosterSlot slot)
        {
            return slot.State switch {
                BoosterSlotState.Running => FormatRunning(slot.Remaining),
                BoosterSlotState.Blocked => GLYPH_BLOCKED,
                BoosterSlotState.Used => GLYPH_USED,
                _ => GLYPH_READY,
            };
        }

        public static float GetRingFill(in BoosterSlot slot)
        {
            if (slot.State != BoosterSlotState.Running || slot.Duration <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(slot.Remaining / slot.Duration);
        }

        public static bool IsTappable(BoosterSlotState state)
        {
            return state == BoosterSlotState.Ready;
        }

        public static bool IsShown(BoosterSlotState state)
        {
            return state != BoosterSlotState.NotEquipped;
        }

        public static Color GetBackground(BoosterSlotState state)
        {
            return state switch {
                BoosterSlotState.Running => UiPalette.Primary,
                BoosterSlotState.Blocked => UiPalette.SwitchOff,
                BoosterSlotState.Used => UiPalette.Soft,
                _ => UiPalette.Sun,
            };
        }

        public static Color GetInk(BoosterSlotState state)
        {
            return state switch {
                BoosterSlotState.Running => UiPalette.White,
                BoosterSlotState.Blocked => UiPalette.Muted,
                BoosterSlotState.Used => UiPalette.Muted,
                _ => UiPalette.Ink,
            };
        }
    }
}
