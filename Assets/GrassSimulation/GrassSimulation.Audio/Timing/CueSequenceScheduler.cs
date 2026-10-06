using EncosyTower.Collections;
using UnityEngine;

namespace GrassSimulation.Audio
{
    public sealed class CueSequenceScheduler : IClearable
    {
        private const int CAPACITY = 8;

        private readonly SoundId[] _ids = new SoundId[CAPACITY];
        private readonly int[] _indices = new int[CAPACITY];
        private readonly float[] _times = new float[CAPACITY];

        private int _count;
        private float _sequenceEnd;
        private float _notBefore;

        public int PendingCount => _count;

        public void BlockUntil(float time)
        {
            _notBefore = Mathf.Max(_notBefore, time);
        }

        public void Schedule(SoundId id, int count, float now, float step)
        {
            var start = Mathf.Max(now, Mathf.Max(_sequenceEnd, _notBefore));

            for (var i = 0; i < count && _count < CAPACITY; i++)
            {
                _ids[_count] = id;
                _indices[_count] = i;
                _times[_count] = start + i * step;
                _count++;
            }

            if (count > 0)
            {
                _sequenceEnd = start + count * step;
            }
        }

        public bool TryTakeDue(float now, out SoundId id, out int index)
        {
            var best = -1;

            for (var i = 0; i < _count; i++)
            {
                if (_times[i] <= now && (best < 0 || _times[i] < _times[best]))
                {
                    best = i;
                }
            }

            if (best < 0)
            {
                id = default;
                index = 0;
                return false;
            }

            id = _ids[best];
            index = _indices[best];

            _count--;
            _ids[best] = _ids[_count];
            _indices[best] = _indices[_count];
            _times[best] = _times[_count];
            return true;
        }

        public void Clear()
        {
            _count = 0;
            _sequenceEnd = 0f;
            _notBefore = 0f;
        }
    }
}
