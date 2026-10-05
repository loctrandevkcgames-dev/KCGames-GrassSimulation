using EncosyTower.Collections;

namespace GrassSimulation.Gameplay
{
    public sealed class HarvestBatcher : IClearable
    {
        private readonly float _interval;

        private int _cells;
        private int _xp;
        private float _windowStart;

        public HarvestBatcher(float interval)
        {
            _interval = interval;
        }

        private bool HasPending => _cells > 0 || _xp > 0;

        public void Add(int cells, int xp, float time)
        {
            if (HasPending == false)
            {
                _windowStart = time;
            }

            _cells += cells;
            _xp += xp;
        }

        public bool TryFlush(float time, bool force, out HarvestBatchedMsg message)
        {
            var isDue = HasPending && (force || time - _windowStart >= _interval);

            if (isDue == false)
            {
                message = default;
                return false;
            }

            message = new HarvestBatchedMsg(_cells, _xp);
            Clear();
            return true;
        }

        public void Clear()
        {
            _cells = 0;
            _xp = 0;
        }
    }
}
