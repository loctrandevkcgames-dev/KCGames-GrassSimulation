using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class MowerHumPlayer
    {
        private const float FADE_SECONDS = 0.3f;

        private readonly AudioSource _source;
        private readonly float _volume;
        private readonly Vector2 _pitch;

        private float _level;

        public MowerHumPlayer(Transform parent, in MowerHumSettings settings, AudioMixerGroup group)
        {
            var host = new GameObject("MowerHum");

            host.transform.SetParent(parent, worldPositionStays: false);

            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            _source.clip = settings.Clip;
            _source.outputAudioMixerGroup = group;
            _volume = settings.Volume;
            _pitch = settings.Pitch;
        }

        public void Step(bool isRunning, float speed01, float deltaTime)
        {
            var target = isRunning ? _volume : 0f;

            _level = Mathf.MoveTowards(_level, target, _volume / FADE_SECONDS * deltaTime);

            if (_level <= 0f)
            {
                if (_source.isPlaying)
                {
                    _source.Stop();
                }

                return;
            }

            if (_source.isPlaying == false && _source.clip.IsValid())
            {
                _source.Play();
            }

            _source.volume = _level;
            _source.pitch = Mathf.Lerp(_pitch.x, _pitch.y, Mathf.Clamp01(speed01));
        }
    }
}
