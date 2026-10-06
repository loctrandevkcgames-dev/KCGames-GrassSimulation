using EncosyTower.ConfigKeys;
using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class VolumeOptions
    {
        public static ConfigKey<float> GetKey(VolumeOption option)
        {
            return option switch {
                VolumeOption.Music => PlayerOptions.MusicVolume,
                _ => PlayerOptions.SfxVolume,
            };
        }

        public static string GetLabel(VolumeOption option)
        {
            return option switch {
                VolumeOption.Music => UiText.OPTION_MUSIC,
                _ => UiText.OPTION_SFX,
            };
        }

        public static float Read(VolumeOption option)
        {
            return option switch {
                VolumeOption.Music => PlayerOptions.GetMusicVolume(),
                _ => PlayerOptions.GetSfxVolume(),
            };
        }

        public static void Write(VolumeOption option, float value)
        {
            PlayerOptions.SetVolume(GetKey(option), value);
        }
    }
}
