using EncosyTower.ConfigKeys;
using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class PauseOptions
    {
        public static ConfigKey<bool> GetKey(PauseOption option)
        {
            return option switch {
                PauseOption.Sound => PlayerOptions.Sound,
                PauseOption.Haptics => PlayerOptions.Haptics,
                _ => PlayerOptions.ReduceEffects,
            };
        }

        public static string GetLabel(PauseOption option)
        {
            return option switch {
                PauseOption.Sound => UiText.OPTION_SOUND,
                PauseOption.Haptics => UiText.OPTION_HAPTICS,
                _ => UiText.OPTION_REDUCE_EFFECTS,
            };
        }

        public static bool Read(PauseOption option)
        {
            return option switch {
                PauseOption.Sound => PlayerOptions.GetSound(),
                PauseOption.Haptics => PlayerOptions.GetHaptics(),
                _ => PlayerOptions.GetReduceEffects(),
            };
        }

        public static void Write(PauseOption option, bool value)
        {
            PlayerOptions.Set(GetKey(option), value);
        }
    }
}
