using EncosyTower.Collections;
using UnityEngine;

namespace GrassSimulation.Audio
{
    public sealed class CutDensityTracker : IClearable
    {
        private const float MIN_SMOOTHING = 1e-4f;
        private const float REST_DENSITY = 1e-3f;

        private readonly float _smoothing;
        private readonly float _silenceTimeout;

        private float _target;
        private float _value;
        private float _lastPushTime;

        public CutDensityTracker(float smoothing, float silenceTimeout)
        {
            _smoothing = smoothing;
            _silenceTimeout = silenceTimeout;
            Clear();
        }

        public float Value => _value;

        public void Push(int cells, float time)
        {
            _target = cells;
            _lastPushTime = time;
        }

        public float Step(float time, float deltaTime)
        {
            if (time - _lastPushTime > _silenceTimeout)
            {
                _target = 0f;
            }

            var blend = 1f - Mathf.Exp(-deltaTime / Mathf.Max(_smoothing, MIN_SMOOTHING));
            _value += (_target - _value) * blend;

            if (_target <= 0f && _value < REST_DENSITY)
            {
                _value = 0f;
            }

            return _value;
        }

        public void Clear()
        {
            _target = 0f;
            _value = 0f;
            _lastPushTime = float.NegativeInfinity;
        }
    }
}
