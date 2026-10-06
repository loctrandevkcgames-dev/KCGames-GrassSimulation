using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class SfxVoicePool
    {
        private readonly AudioSource[] _sources;
        private readonly float[] _startTime;

        public SfxVoicePool(Transform parent, int count)
        {
            var host = new GameObject("SfxVoices");

            host.transform.SetParent(parent, worldPositionStays: false);

            _sources = new AudioSource[count];
            _startTime = new float[count];

            for (var i = 0; i < count; i++)
            {
                var source = host.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _sources[i] = source;
            }
        }

        public void Play(AudioClip clip, AudioMixerGroup group, float volume, float pitch, float now)
        {
            if (clip.IsInvalid())
            {
                return;
            }

            var index = FindVoice();
            var source = _sources[index];

            source.Stop();
            source.outputAudioMixerGroup = group;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            _startTime[index] = now;
        }

        private int FindVoice()
        {
            var oldest = 0;

            for (var i = 0; i < _sources.Length; i++)
            {
                if (_sources[i].isPlaying == false)
                {
                    return i;
                }

                if (_startTime[i] < _startTime[oldest])
                {
                    oldest = i;
                }
            }

            return oldest;
        }
    }
}
