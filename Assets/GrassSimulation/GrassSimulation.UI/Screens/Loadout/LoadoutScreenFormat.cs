using System;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    public static class LoadoutScreenFormat
    {
        public static float GetBarFill(float value, float best)
        {
            return best > 0f ? Mathf.Clamp01(value / best) : 0f;
        }

        public static MachineStats GetBestStats(in LoadoutSnapshot snapshot)
        {
            var best = default(MachineStats);

            for (var i = 0; i < snapshot.MachineCount; i++)
            {
                var stats = snapshot.GetMachine(i).Stats;

                best = new MachineStats(
                      Math.Max(best.CutRadius, stats.CutRadius)
                    , Math.Max(best.CuttingPower, stats.CuttingPower)
                    , Math.Max(best.Speed, stats.Speed)
                );
            }

            return best;
        }

        public static string FormatUnlockCard(UnlockSettings unlock)
        {
            return string.Format(UiText.UNLOCK_CARD, FormatUnlockName(unlock));
        }

        public static string FormatUnlockName(UnlockSettings unlock)
        {
            return unlock.Kind switch {
                UnlockKind.WideMachine => UiText.UNLOCK_WIDE_MACHINE,
                UnlockKind.ExtraTimeBooster => string.Format(UiText.UNLOCK_EXTRA_TIME, unlock.Amount),
                UnlockKind.TurboBooster => string.Format(UiText.UNLOCK_TURBO, unlock.Amount),
                _ => string.Empty,
            };
        }

        public static bool TryFormatUnlocks(UnlockSettings[] unlocks, out string text)
        {
            if (unlocks == null || unlocks.Length == 0)
            {
                text = null;
                return false;
            }

            text = FormatUnlockCard(unlocks[0]);

            for (var i = 1; i < unlocks.Length; i++)
            {
                text = string.Concat(text, Environment.NewLine, FormatUnlockCard(unlocks[i]));
            }

            return true;
        }
    }
}
