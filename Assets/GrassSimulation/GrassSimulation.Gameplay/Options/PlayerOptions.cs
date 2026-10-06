using EncosyTower.ConfigKeys;

namespace GrassSimulation.Gameplay
{
    public static class PlayerOptions
    {
        public const bool DEFAULT_SOUND = true;
        public const bool DEFAULT_HAPTICS = true;
        public const bool DEFAULT_REDUCE_EFFECTS = false;

        public static readonly ConfigKey<bool> Sound = new("options.sound");
        public static readonly ConfigKey<bool> Haptics = new("options.haptics");
        public static readonly ConfigKey<bool> ReduceEffects = new("options.reduce-effects");

        public static bool Get(ConfigKey<bool> key, bool defaultValue)
            => key.GetPlayerPref(defaultValue);

        public static void Set(ConfigKey<bool> key, bool value)
            => key.SetPlayerPref(value);

        public static bool GetSound()
            => Get(Sound, DEFAULT_SOUND);

        public static bool GetHaptics()
            => Get(Haptics, DEFAULT_HAPTICS);

        public static bool GetReduceEffects()
            => Get(ReduceEffects, DEFAULT_REDUCE_EFFECTS);
    }
}
