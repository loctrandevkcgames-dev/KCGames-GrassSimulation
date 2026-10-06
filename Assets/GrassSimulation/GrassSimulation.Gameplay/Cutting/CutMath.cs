using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public static class CutMath
    {
        public const float DEFAULT_SLOW_HINT = 0.8f;

        private const float MIN_SEGMENT = 1e-4f;

        public static float CenterSpeed(float bladeRadius, float zoneRadius, float cuttingPower, float toughness)
            => 2f * (bladeRadius + zoneRadius) * cuttingPower / toughness;

        public static float CutTime(float toughness, float cuttingPower)
            => toughness / cuttingPower;

        public static float Swath(float bladeRadius, float zoneRadius, float speed, float cutTime)
        {
            var reach = bladeRadius + zoneRadius;
            var halfTravel = speed * cutTime * 0.5f;
            var squared = reach * reach - halfTravel * halfTravel;

            return squared > 0f ? 2f * Mathf.Sqrt(squared) : 0f;
        }

        public static Vector2 ClosestPoint(Vector2 from, Vector2 to, Vector2 point)
        {
            var segment = to - from;
            var squaredLength = segment.sqrMagnitude;

            if (squaredLength < MIN_SEGMENT * MIN_SEGMENT)
            {
                return from;
            }

            var along = Mathf.Clamp01(Vector2.Dot(point - from, segment) / squaredLength);
            return from + segment * along;
        }

        public static float ContactCoverage(Vector2 from, Vector2 to, Vector2 point, float radius)
        {
            var segment = to - from;
            var length = segment.magnitude;

            if (length < MIN_SEGMENT)
            {
                return (point - to).sqrMagnitude <= radius * radius ? 1f : 0f;
            }

            var direction = segment / length;
            var offset = point - from;
            var along = Vector2.Dot(offset, direction);
            var across = Mathf.Abs(direction.x * offset.y - direction.y * offset.x);

            if (across > radius)
            {
                return 0f;
            }

            var halfChord = Mathf.Sqrt(radius * radius - across * across);
            var enter = Mathf.Max(along - halfChord, 0f);
            var exit = Mathf.Min(along + halfChord, length);
            return Mathf.Max(exit - enter, 0f) / length;
        }
    }
}
