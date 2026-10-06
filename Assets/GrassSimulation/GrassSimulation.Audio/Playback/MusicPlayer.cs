using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class MusicPlayer
    {
        private const int VOICE_COUNT = 2;

        private readonly AudioSource[] _sources = new AudioSource[VOICE_COUNT];
        private readonly VolumeFade[] _fades = new VolumeFade[VOICE_COUNT];
        private readonly float _duckDecibels;
        private readonly float _duckSeconds;
        private readonly float _duckAttack;
        private readonly float _duckRelease;

        private DuckEnvelope _duck;
        private AudioClip _clip;
        private int _active = VOICE_COUNT - 1;

        public MusicPlayer(Transform parent, AudioLibrary library, AudioMixerGroup group)
        {
            var host = new GameObject("Music");

            host.transform.SetParent(parent, worldPositionStays: false);

            for (var i = 0; i < VOICE_COUNT; i++)
            {
                var source = host.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.volume = 0f;
                source.outputAudioMixerGroup = group;
                _sources[i] = source;
            }

            _duckDecibels = library.DuckDecibels;
            _duckSeconds = library.DuckSeconds;
            _duckAttack = library.DuckAttackSeconds;
            _duckRelease = library.DuckReleaseSeconds;
        }

        public void Play(AudioClip clip, float fadeSeconds)
        {
            if (clip.IsInvalid())
            {
                FadeOut(fadeSeconds);
                return;
            }

            if (_clip == clip)
            {
                return;
            }

            FadeOutVoice(_active, fadeSeconds);

            _active = (_active + 1) % VOICE_COUNT;

            var source = _sources[_active];

            source.Stop();
            source.clip = clip;
            source.volume = 0f;
            source.Play();
            _fades[_active].Start(from: 0f, to: 1f, seconds: fadeSeconds);
            _clip = clip;
        }

        public void FadeOut(float fadeSeconds)
        {
            FadeOutVoice(_active, fadeSeconds);
            _clip = null;
        }

        public void Duck(float now)
        {
            _duck.Trigger(now, _duckSeconds);
        }

        public void Step(float now, float deltaTime)
        {
            var gain = _duck.Gain(now, _duckDecibels, _duckAttack, _duckRelease);

            for (var i = 0; i < VOICE_COUNT; i++)
            {
                var source = _sources[i];

                _fades[i].Step(deltaTime);
                source.volume = _fades[i].Value * gain;

                if (_fades[i].IsDone && _fades[i].Value <= 0f && source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        private void FadeOutVoice(int index, float fadeSeconds)
        {
            _fades[index].Start(from: _fades[index].Value, to: 0f, seconds: fadeSeconds);
        }
    }
}
