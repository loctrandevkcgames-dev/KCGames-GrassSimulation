using System;
using EncosyTower.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace GrassSimulation.Audio
{
    public sealed class CueGate : IClearable
    {
        private const int NO_VARIANT = -1;

        private readonly float[] _lastTime = new float[SoundIdExtensions.Length];
        private readonly int[] _lastVariant = new int[SoundIdExtensions.Length];

        private Unity.Mathematics.Random _random;

        public CueGate(uint seed)
        {
            _random = new Unity.Mathematics.Random(seed == 0u ? 1u : seed);
            Clear();
        }

        public bool TryPick(
              SoundId id
            , int clipCount
            , CueVariantOrder order
            , Vector2 pitch
            , float cooldown
            , float now
            , out CuePick pick
        )
        {
            if (IsReady(id, clipCount, cooldown, now) == false)
            {
                pick = default;
                return false;
            }

            var last = _lastVariant[(int)id];
            var variant = order switch {
                CueVariantOrder.Sequence => (last + 1) % clipCount,
                CueVariantOrder.Indexed => 0,
                _ => PickRandom(clipCount, last),
            };

            pick = Commit(id, variant, pitch, now);
            return true;
        }

        public bool TryPick(
              SoundId id
            , int clipCount
            , int index
            , Vector2 pitch
            , float cooldown
            , float now
            , out CuePick pick
        )
        {
            if (IsReady(id, clipCount, cooldown, now) == false)
            {
                pick = default;
                return false;
            }

            pick = Commit(id, Mathf.Clamp(index, min: 0, max: clipCount - 1), pitch, now);
            return true;
        }

        public void Clear()
        {
            Array.Fill(_lastTime, float.NegativeInfinity);
            Array.Fill(_lastVariant, NO_VARIANT);
        }

        private bool IsReady(SoundId id, int clipCount, float cooldown, float now)
        {
            return clipCount > 0 && now - _lastTime[(int)id] >= cooldown;
        }

        private CuePick Commit(SoundId id, int variant, Vector2 pitch, float now)
        {
            _lastTime[(int)id] = now;
            _lastVariant[(int)id] = variant;

            var value = pitch.x < pitch.y ? _random.NextFloat(pitch.x, pitch.y) : pitch.x;
            return new CuePick(variant, value);
        }

        private int PickRandom(int clipCount, int last)
        {
            if (clipCount == 1)
            {
                return 0;
            }

            if (last == NO_VARIANT)
            {
                return _random.NextInt(0, clipCount);
            }

            var pick = _random.NextInt(0, clipCount - 1);
            return pick >= last ? pick + 1 : pick;
        }
    }
}
