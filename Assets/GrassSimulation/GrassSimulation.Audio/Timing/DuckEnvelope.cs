using UnityEngine;

namespace GrassSimulation.Audio
{
    public struct DuckEnvelope
    {
        private const float MIN_SECONDS = 1e-4f;

        private float _holdUntil;
        private float _decibels;
        private float _lastTime;
        private bool _hasTime;

        public void Trigger(float now, float hold)
        {
            _holdUntil = Mathf.Max(_holdUntil, now + hold);
        }

        public float Gain(float now, float duckDecibels, float attackSeconds, float releaseSeconds)
        {
            var deltaTime = _hasTime ? Mathf.Max(now - _lastTime, 0f) : 0f;
            var depth = Mathf.Abs(duckDecibels);

            _lastTime = now;
            _hasTime = true;

            if (now < _holdUntil)
            {
                var attackStep = depth / Mathf.Max(attackSeconds, MIN_SECONDS) * deltaTime;
                _decibels = Mathf.Max(duckDecibels, _decibels - attackStep);
            }
            else
            {
                var releaseStep = depth / Mathf.Max(releaseSeconds, MIN_SECONDS) * deltaTime;
                _decibels = Mathf.Min(0f, _decibels + releaseStep);
            }

            return AudioVolume.FromDecibels(_decibels);
        }
    }
}
