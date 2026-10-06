using EncosyTower.UnityExtensions;
using UnityEngine;
using UnityEngine.Audio;

namespace GrassSimulation.Audio
{
    public sealed class GrassCutLoopPlayer
    {
        private const float RATE_SECONDS = 0.3f;
        private const float ATTACK_SECONDS = 0.12f;
        private const float RELEASE_SECONDS = 0.45f;
        private const float MIN_FULL_RATE = 1f;

        private readonly AudioSource _source;
        private readonly float _volume;
        private readonly Vector2 _pitch;
        private readonly float _fullCellsPerSecond;

        private float _cutEnergy;
        private float _density;

        public GrassCutLoopPlayer(Transform parent, in GrassCutLoopSettings settings, AudioMixerGroup group)
        {
            var host = new GameObject("GrassCutLoop");

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
            _fullCellsPerSecond = Mathf.Max(settings.FullCellsPerSecond, MIN_FULL_RATE);
        }

        public void AddCells(int cells)
        {
            _cutEnergy += cells;
        }

        public void Step(bool isRunning, float deltaTime)
        {
            _cutEnergy *= Mathf.Exp(-deltaTime / RATE_SECONDS);

            var rate = _cutEnergy / RATE_SECONDS;
            var target = isRunning ? Mathf.Clamp01(rate / _fullCellsPerSecond) : 0f;
            var fadeSeconds = target > _density ? ATTACK_SECONDS : RELEASE_SECONDS;

            _density = Mathf.MoveTowards(_density, target, deltaTime / fadeSeconds);

            if (_density <= 0f)
            {
                if (_source.isPlaying)
                {
                    _source.Stop();
                }

                return;
            }

            if (_source.isPlaying == false && _source.clip.IsValid())
            {
                _source.time = Random.Range(0f, _source.clip.length);
                _source.Play();
            }

            _source.volume = _volume * Mathf.Sqrt(_density);
            _source.pitch = Mathf.Lerp(_pitch.x, _pitch.y, _density);
        }
    }
}
