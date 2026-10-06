using System;
using EncosyTower.Collections;
using Unity.Collections;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class FieldFeedback : IClearable
    {
        private const float SHAKE_DECAY = 4f;
        private const float LOCK_FLASH_DECAY = 3f;
        private const float PROTECTED_FLASH_DECAY = 2f;
        private const byte FULL = 255;
        private const float PARTIAL_SCALE = 254f;
        private const float CUT_POP_SECONDS = 0.12f;
        private const float LITTER_APPEAR_SECONDS = 0.25f;
        private const float LITTER_FRESH_SECONDS = 2.5f;
        private const float SETTLED_AGE = 1000f;
        private const float MIN_HEADING = 1e-4f;

        private readonly float[] _shake;
        private readonly float[] _lockFlash;
        private readonly float[] _protectedFlash;
        private readonly float[] _cutAge;
        private readonly Vector2[] _cutHeading;
        private readonly Color[] _litterLeafByKind = new Color[PlantKindExtensions.Length];
        private readonly Color[] _litterAccentByKind = new Color[PlantKindExtensions.Length];

        public FieldFeedback(int cellCount)
        {
            _shake = new float[cellCount];
            _lockFlash = new float[cellCount];
            _protectedFlash = new float[cellCount];
            _cutAge = new float[cellCount];
            _cutHeading = new Vector2[cellCount];

            Array.Fill(_cutAge, SETTLED_AGE);
        }

        private static byte ToByte(float value)
            => (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * FULL);

        private static byte ToClearanceByte(float progress)
            => progress >= 1f ? FULL : (byte)(Mathf.Clamp01(progress) * PARTIAL_SCALE);

        private static Color32 ToPremultiplied(Color color, float amount, float alpha)
            => new(ToByte(color.r * amount), ToByte(color.g * amount), ToByte(color.b * amount), ToByte(alpha));

        private static float ToSigned01(float value)
            => value * 0.5f + 0.5f;

        public void SetLitterColors(PlantKind kind, Color leaf, Color accent, float accentShare)
        {
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                leaf = leaf.linear;
                accent = accent.linear;
            }

            _litterLeafByKind[(int)kind] = leaf;
            _litterAccentByKind[(int)kind] = new Color(accent.r, accent.g, accent.b, Mathf.Clamp01(accentShare));
        }

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

        public void MarkCut(int index, Vector2 heading)
        {
            _cutAge[index] = 0f;
            _cutHeading[index] = heading.sqrMagnitude > MIN_HEADING ? heading.normalized : Vector2.zero;
        }

        public void Clear()
        {
            Array.Clear(_shake, 0, _shake.Length);
            Array.Clear(_lockFlash, 0, _lockFlash.Length);
            Array.Clear(_protectedFlash, 0, _protectedFlash.Length);
            Array.Clear(_cutHeading, 0, _cutHeading.Length);
            Array.Fill(_cutAge, SETTLED_AGE);
        }

        public void Decay(float deltaTime)
        {
            var count = _shake.Length;

            for (var i = 0; i < count; i++)
            {
                _shake[i] = Mathf.Max(_shake[i] - SHAKE_DECAY * deltaTime, 0f);
                _lockFlash[i] = Mathf.Max(_lockFlash[i] - LOCK_FLASH_DECAY * deltaTime, 0f);
                _protectedFlash[i] = Mathf.Max(_protectedFlash[i] - PROTECTED_FLASH_DECAY * deltaTime, 0f);
                _cutAge[i] = Mathf.Min(_cutAge[i] + deltaTime, SETTLED_AGE);
            }
        }

        public void Write(
              FieldGrid grid
            , NativeArray<Color32> states
            , NativeArray<Color32> litter
            , NativeArray<Color32> litterAccent
            , NativeArray<Color32> cutStates
        )
        {
            var count = grid.Count;

            for (var i = 0; i < count; i++)
            {
                var kind = grid.GetKind(i);
                var isEmpty = kind == PlantKind.None;
                var progress = grid.GetProgress(i);
                var clearance = isEmpty ? FULL : ToClearanceByte(progress);
                var shake = ToByte(_shake[i]);
                var lockFlash = ToByte(_lockFlash[i]);
                var protectedFlash = ToByte(_protectedFlash[i]);
                states[i] = new Color32(clearance, shake, lockFlash, protectedFlash);

                var age = _cutAge[i];
                var appear = Mathf.Clamp01(age / LITTER_APPEAR_SECONDS);
                var amount = isEmpty ? 0f : Mathf.Clamp01(progress) * appear;
                var accent = _litterAccentByKind[(int)kind];
                litter[i] = ToPremultiplied(_litterLeafByKind[(int)kind], amount, amount);
                litterAccent[i] = ToPremultiplied(accent, amount, accent.a * amount);

                var heading = _cutHeading[i];
                var fresh = 1f - Mathf.Clamp01(age / LITTER_FRESH_SECONDS);
                var pop = isEmpty ? 0f : 1f - Mathf.Clamp01(age / CUT_POP_SECONDS);
                cutStates[i] = new Color32(
                      ToByte(ToSigned01(heading.x) * amount)
                    , ToByte(ToSigned01(heading.y) * amount)
                    , ToByte(fresh * amount)
                    , ToByte(pop)
                );
            }
        }
    }
}
