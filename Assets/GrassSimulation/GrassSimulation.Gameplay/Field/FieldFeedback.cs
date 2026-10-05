using System;
using Unity.Collections;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class FieldFeedback
    {
        private const float SHAKE_DECAY = 4f;
        private const float LOCK_FLASH_DECAY = 3f;
        private const float PROTECTED_FLASH_DECAY = 2f;
        private const byte FULL = 255;
        private const float PARTIAL_SCALE = 254f;

        private readonly float[] _shake;
        private readonly float[] _lockFlash;
        private readonly float[] _protectedFlash;

        public FieldFeedback(int cellCount)
        {
            _shake = new float[cellCount];
            _lockFlash = new float[cellCount];
            _protectedFlash = new float[cellCount];
        }

        private static byte ToByte(float value)
            => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * FULL);

        private static byte ToClearanceByte(float progress)
            => progress >= 1f ? FULL : (byte)(Mathf.Clamp01(progress) * PARTIAL_SCALE);

        public void Shake(int index)
        {
            _shake[index] = 1f;
        }

        public void FlashLocked(int index)
        {
            _lockFlash[index] = 1f;
        }

        public void FlashProtected(int index)
        {
            _protectedFlash[index] = 1f;
        }

        public void Clear()
        {
            Array.Clear(_shake, 0, _shake.Length);
            Array.Clear(_lockFlash, 0, _lockFlash.Length);
            Array.Clear(_protectedFlash, 0, _protectedFlash.Length);
        }

        public void Decay(float deltaTime)
        {
            var count = _shake.Length;

            for (var i = 0; i < count; i++)
            {
                _shake[i] = Mathf.Max(_shake[i] - SHAKE_DECAY * deltaTime, 0f);
                _lockFlash[i] = Mathf.Max(_lockFlash[i] - LOCK_FLASH_DECAY * deltaTime, 0f);
                _protectedFlash[i] = Mathf.Max(_protectedFlash[i] - PROTECTED_FLASH_DECAY * deltaTime, 0f);
            }
        }

        public void Write(FieldGrid grid, NativeArray<Color32> states)
        {
            var count = grid.Count;

            for (var i = 0; i < count; i++)
            {
                var clearance = grid.GetKind(i) == PlantKind.None ? FULL : ToClearanceByte(grid.GetProgress(i));
                var shake = ToByte(_shake[i]);
                var lockFlash = ToByte(_lockFlash[i]);
                var protectedFlash = ToByte(_protectedFlash[i]);
                states[i] = new Color32(clearance, shake, lockFlash, protectedFlash);
            }
        }
    }
}
