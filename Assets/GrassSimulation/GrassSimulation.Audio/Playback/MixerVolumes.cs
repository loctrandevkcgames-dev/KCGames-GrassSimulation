using GrassSimulation.Gameplay;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class MixerVolumes
    {
        private readonly AudioMixer _mixer;
        private readonly float _masterBase;
        private readonly float _musicBase;
        private readonly float _ambienceBase;
        private readonly float _sfxBase;
        private readonly float _uiBase;

        public MixerVolumes(AudioMixer mixer)
        {
            _mixer = mixer;
            _masterBase = Capture(AudioMixerParams.MASTER);
            _musicBase = Capture(AudioMixerParams.MUSIC);
            _ambienceBase = Capture(AudioMixerParams.AMBIENCE);
            _sfxBase = Capture(AudioMixerParams.SFX);
            _uiBase = Capture(AudioMixerParams.UI);
        }

        public void Apply()
        {
            Apply(PlayerOptions.GetSound(), PlayerOptions.GetMusicVolume(), PlayerOptions.GetSfxVolume());
        }

        public void Apply(bool isSoundOn, float music, float sfx)
        {
            Set(AudioMixerParams.MASTER, AudioVolume.BusDecibels(_masterBase, slider: 1f, muted: isSoundOn == false));
            Set(AudioMixerParams.MUSIC, AudioVolume.BusDecibels(_musicBase, music, muted: false));
            Set(AudioMixerParams.AMBIENCE, AudioVolume.BusDecibels(_ambienceBase, music, muted: false));
            Set(AudioMixerParams.SFX, AudioVolume.BusDecibels(_sfxBase, sfx, muted: false));
            Set(AudioMixerParams.UI, AudioVolume.BusDecibels(_uiBase, sfx, muted: false));
        }

        private float Capture(string parameter)
        {
            _mixer.ClearFloat(parameter);

            if (_mixer.GetFloat(parameter, out var decibels))
            {
                return decibels;
            }

            ThrowHelper.LogWarning($"The audio mixer has no exposed parameter '{parameter}'.");
            return 0f;
        }

        private void Set(string parameter, float decibels)
        {
            _mixer.SetFloat(parameter, decibels);
        }
    }
}
