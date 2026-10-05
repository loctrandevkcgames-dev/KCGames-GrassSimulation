namespace GrassSimulation.Gameplay
{
    public sealed class ProtectedRule
    {
        private readonly float _cooldown;

        private float _lastTouchTime;

        public ProtectedRule(float cooldown)
        {
            _cooldown = cooldown;
            Reset();
        }

        public int Hits { get; private set; }

        public bool Touch(float time)
        {
            var isNewHit = time - _lastTouchTime > _cooldown;

            if (isNewHit)
            {
                Hits++;
            }

            _lastTouchTime = time;
            return isNewHit;
        }

        public void Reset()
        {
            Hits = 0;
            _lastTouchTime = float.NegativeInfinity;
        }
    }
}
