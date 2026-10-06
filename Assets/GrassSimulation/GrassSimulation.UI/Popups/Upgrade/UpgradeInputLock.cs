namespace GrassSimulation.UI
{
    public struct UpgradeInputLock
    {
        public const float DURATION = 0.4f;

        private float _lockedUntil;

        public void Start(float now)
        {
            _lockedUntil = now + DURATION;
        }

        public readonly bool IsLocked(float now)
        {
            return now < _lockedUntil;
        }
    }
}
