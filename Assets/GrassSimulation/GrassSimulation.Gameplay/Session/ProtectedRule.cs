using System;

namespace GrassSimulation.Gameplay
{
    public sealed class ProtectedRule
    {
        private readonly float _retrigger;

        private float[] _lastTouchTimes;

        public ProtectedRule(float retrigger, int bedCount)
        {
            _retrigger = retrigger;
            _lastTouchTimes = new float[Math.Max(bedCount, 1)];
            Reset();
        }

        public int Hits { get; private set; }

        public bool Touch(int bed, float time)
        {
            if (bed >= _lastTouchTimes.Length)
            {
                var previousLength = _lastTouchTimes.Length;

                Array.Resize(ref _lastTouchTimes, bed + 1);
                var added = _lastTouchTimes.Length - previousLength;

                Array.Fill(_lastTouchTimes, float.NegativeInfinity, previousLength, added);
            }

            var isNewHit = time - _lastTouchTimes[bed] > _retrigger;

            if (isNewHit)
            {
                Hits++;
            }

            _lastTouchTimes[bed] = time;
            return isNewHit;
        }

        public void Reset()
        {
            Hits = 0;
            Array.Fill(_lastTouchTimes, float.NegativeInfinity);
        }
    }
}
