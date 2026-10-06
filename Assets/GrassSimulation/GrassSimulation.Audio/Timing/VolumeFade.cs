using UnityEngine;

namespace GrassSimulation.Audio
{
    public struct VolumeFade
    {
        private float _from;
        private float _to;
        private float _seconds;
        private float _elapsed;

        public float Value { get; private set; }

        public bool IsDone => _elapsed >= _seconds;

        public void Start(float from, float to, float seconds)
        {
            _from = from;
            _to = to;
            _seconds = Mathf.Max(seconds, 0f);
            _elapsed = 0f;
            Value = _seconds > 0f ? from : to;
        }

        public void Step(float deltaTime)
        {
            if (IsDone)
            {
                Value = _to;
                return;
            }

            _elapsed = Mathf.Min(_elapsed + deltaTime, _seconds);
            Value = Mathf.Lerp(_from, _to, _elapsed / _seconds);
        }
    }
}
