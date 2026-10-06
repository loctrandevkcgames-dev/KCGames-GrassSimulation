using UnityEngine;

namespace GrassSimulation.UI
{
    public static class PreviewViewport
    {
        private const float MIN_SIZE = 0.01f;
        private const float SAME_TOLERANCE = 1e-3f;

        public static bool TryFromScreen(Vector2 min, Vector2 max, Vector2 screenSize, out Rect viewport)
        {
            viewport = default;

            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return false;
            }

            var xMin = Mathf.Clamp01(min.x / screenSize.x);
            var yMin = Mathf.Clamp01(min.y / screenSize.y);
            var xMax = Mathf.Clamp01(max.x / screenSize.x);
            var yMax = Mathf.Clamp01(max.y / screenSize.y);

            if (xMax - xMin < MIN_SIZE || yMax - yMin < MIN_SIZE)
            {
                return false;
            }

            viewport = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            return true;
        }

        public static bool IsSame(Rect a, Rect b)
        {
            return Mathf.Abs(a.xMin - b.xMin) <= SAME_TOLERANCE
                && Mathf.Abs(a.yMin - b.yMin) <= SAME_TOLERANCE
                && Mathf.Abs(a.xMax - b.xMax) <= SAME_TOLERANCE
                && Mathf.Abs(a.yMax - b.yMax) <= SAME_TOLERANCE;
        }
    }
}
