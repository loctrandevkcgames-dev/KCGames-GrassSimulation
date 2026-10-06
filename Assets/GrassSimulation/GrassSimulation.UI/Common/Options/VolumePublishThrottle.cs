using UnityEngine;

namespace GrassSimulation.UI
{
    public struct VolumePublishThrottle
    {
        public const float INTERVAL = 0.1f;

        private float _cooldown;
        private bool _isPending;

        public void MarkChanged()
        {
            _isPending = true;
        }

        public bool Tick(float deltaTime)
        {
            _cooldown = Mathf.Max(0f, _cooldown - deltaTime);

            if (_isPending == false || _cooldown > 0f)
            {
                return false;
            }

            _isPending = false;
            _cooldown = INTERVAL;

            return true;
        }

        public bool Flush()
        {
            var wasPending = _isPending;

            _isPending = false;
            _cooldown = 0f;

            return wasPending;
        }
    }
}
