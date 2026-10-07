using EncosyTower.ConfigKeys;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public static class PlayerOptions
    {
        public const bool DEFAULT_SOUND = true;
        public const bool DEFAULT_HAPTICS = true;
        public const bool DEFAULT_REDUCE_EFFECTS = false;
        public const bool DEFAULT_BOOSTER_LEFT = false;
        public const float DEFAULT_MUSIC_VOLUME = 1f;
        public const float DEFAULT_SFX_VOLUME = 1f;

        public static readonly ConfigKey<bool> Sound = new("options.sound");
        public static readonly ConfigKey<bool> Haptics = new("options.haptics");
        public static readonly ConfigKey<bool> ReduceEffects = new("options.reduce-effects");
        public static readonly ConfigKey<bool> BoosterLeft = new("options.booster-left");
        public static readonly ConfigKey<float> MusicVolume = new("options.music-volume");
        public static readonly ConfigKey<float> SfxVolume = new("options.sfx-volume");

        public static bool Get(ConfigKey<bool> key, bool defaultValue)
            => key.GetPlayerPref(defaultValue);

        public static void Set(ConfigKey<bool> key, bool value)
            => key.SetPlayerPref(value);

        public static float Get(ConfigKey<float> key, float defaultValue)
            => key.GetPlayerPref(defaultValue);

        public static void SetVolume(ConfigKey<float> key, float value)
            => key.SetPlayerPref(Mathf.Clamp01(value));

        public static bool GetSound()
            => Get(Sound, DEFAULT_SOUND);

        public static bool GetHaptics()
            => Get(Haptics, DEFAULT_HAPTICS);

        public static bool GetReduceEffects()
            => Get(ReduceEffects, DEFAULT_REDUCE_EFFECTS);

        public static bool GetBoosterLeft()
            => Get(BoosterLeft, DEFAULT_BOOSTER_LEFT);

        public static float GetMusicVolume()
            => Mathf.Clamp01(Get(MusicVolume, DEFAULT_MUSIC_VOLUME));

        public static float GetSfxVolume()
            => Mathf.Clamp01(Get(SfxVolume, DEFAULT_SFX_VOLUME));
    }
}
