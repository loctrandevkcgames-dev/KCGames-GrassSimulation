using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class AmbiencePlayer
    {
        private readonly AudioSource[] _sources;
        private readonly float[] _volumes;

        private VolumeFade _fade;

        public AmbiencePlayer(Transform parent, AmbienceLayer[] layers, AudioMixerGroup group)
        {
            var host = new GameObject("Ambience");
            var count = layers == null ? 0 : layers.Length;

            host.transform.SetParent(parent, worldPositionStays: false);

            _sources = new AudioSource[count];
            _volumes = new float[count];

            for (var i = 0; i < count; i++)
            {
                var source = host.AddComponent<AudioSource>();

                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.volume = 0f;
                source.clip = layers[i].Clip;
                source.outputAudioMixerGroup = group;
                _sources[i] = source;
                _volumes[i] = layers[i].Volume;
            }
        }

        public void Start(float fadeSeconds)
        {
            _fade.Start(from: 0f, to: 1f, seconds: fadeSeconds);

            for (var i = 0; i < _sources.Length; i++)
            {
                if (_sources[i].clip.IsValid())
                {
                    _sources[i].Play();
                }
            }
        }

        public void Step(float deltaTime)
        {
            _fade.Step(deltaTime);

            for (var i = 0; i < _sources.Length; i++)
            {
                _sources[i].volume = _volumes[i] * _fade.Value;
            }
        }
    }
}
