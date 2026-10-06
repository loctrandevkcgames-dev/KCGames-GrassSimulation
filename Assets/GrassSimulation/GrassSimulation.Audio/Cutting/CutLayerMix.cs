using UnityEngine;

namespace GrassSimulation.Audio
{
    public static class CutLayerMix
    {
        private const float QUARTER_TURN = Mathf.PI * 0.5f;
        private const float MIN_SPAN = 1e-4f;

        public static CutLayerWeights Evaluate(float density, in CutLayerThresholds thresholds)
        {
            if (density <= 0f)
            {
                return default;
            }

            if (density < thresholds.Light)
            {
                return new CutLayerWeights(Rise(density / Mathf.Max(thresholds.Light, MIN_SPAN)), 0f, 0f);
            }

            if (density < thresholds.Medium)
            {
                var t = Progress(density, thresholds.Light, thresholds.Medium);
                return new CutLayerWeights(Fall(t), Rise(t), 0f);
            }

            if (density < thresholds.Dense)
            {
                var t = Progress(density, thresholds.Medium, thresholds.Dense);
                return new CutLayerWeights(0f, Fall(t), Rise(t));
            }

            return new CutLayerWeights(0f, 0f, 1f);
        }

        public static float Pitch(float density01, Vector2 range)
            => Mathf.Lerp(range.x, range.y, Mathf.Clamp01(density01));

        private static float Progress(float value, float from, float to)
            => Mathf.Clamp01((value - from) / Mathf.Max(to - from, MIN_SPAN));

        private static float Rise(float t)
            => Mathf.Sin(t * QUARTER_TURN);

        private static float Fall(float t)
            => Mathf.Cos(t * QUARTER_TURN);
    }
}
