using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class CutLoopPlayer
    {
        private const int LAYER_COUNT = 3;
        private const float FADE_SECONDS = 0.15f;

        private readonly AudioSource[] _sources = new AudioSource[LAYER_COUNT];

        private float _gain;

        public CutLoopPlayer(Transform parent, AudioLibrary library, AudioMixerGroup group)
        {
            var host = new GameObject("CutLoops");

            host.transform.SetParent(parent, worldPositionStays: false);

            _sources[0] = CreateSource(host, library.CutLoopLight, group);
            _sources[1] = CreateSource(host, library.CutLoopMedium, group);
            _sources[2] = CreateSource(host, library.CutLoopDense, group);
        }

        public void Step(in CutLayerWeights weights, float pitch, bool isActive, float deltaTime)
        {
            _gain = Mathf.MoveTowards(_gain, isActive ? 1f : 0f, deltaTime / FADE_SECONDS);

            if (_gain <= 0f)
            {
                StopAll();
                return;
            }

            StartAll();

            _sources[0].volume = weights.Light * _gain;
            _sources[1].volume = weights.Medium * _gain;
            _sources[2].volume = weights.Dense * _gain;

            for (var i = 0; i < LAYER_COUNT; i++)
            {
                _sources[i].pitch = pitch;
            }
        }

        private static AudioSource CreateSource(GameObject host, AudioClip clip, AudioMixerGroup group)
        {
            var source = host.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.clip = clip;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private void StartAll()
        {
            for (var i = 0; i < LAYER_COUNT; i++)
            {
                var source = _sources[i];

                if (source.isPlaying == false && source.clip.IsValid())
                {
                    source.volume = 0f;
                    source.Play();
                }
            }
        }

        private void StopAll()
        {
            for (var i = 0; i < LAYER_COUNT; i++)
            {
                if (_sources[i].isPlaying)
                {
                    _sources[i].Stop();
                }
            }
        }
    }
}
