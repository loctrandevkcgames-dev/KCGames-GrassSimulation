using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public readonly record struct ObstacleShape(ObstacleKind Kind, Vector2 A, Vector2 B, float Radius)
    {
        public const float FENCE_RADIUS = 0.12f;

        public static ObstacleShape Circle(ObstacleKind kind, Vector2 center, float radius)
            => new(kind, center, center, radius);

        public static ObstacleShape Capsule(ObstacleKind kind, Vector2 a, Vector2 b, float radius)
            => new(kind, a, b, radius);

        public static ObstacleShape From(in ObstaclePlacement placement, Vector2 offset)
        {
            var center = placement.Position + offset;

            if (placement.Kind == ObstacleKind.Rock)
            {
                return Circle(placement.Kind, center, placement.Length * 0.5f);
            }

            var radians = placement.Yaw * Mathf.Deg2Rad;
            var half = new Vector2(Mathf.Cos(radians), -Mathf.Sin(radians)) * (placement.Length * 0.5f);

            return Capsule(placement.Kind, center - half, center + half, FENCE_RADIUS);
        }

        public Vector2 Closest(Vector2 point)
            => CutMath.ClosestPoint(A, B, point);

        public bool Overlaps(Vector2 min, Vector2 max)
        {
            var low = Vector2.Min(A, B) - Vector2.one * Radius;
            var high = Vector2.Max(A, B) + Vector2.one * Radius;

            return low.x <= max.x && high.x >= min.x && low.y <= max.y && high.y >= min.y;
        }

        public bool IsBlocking(Vector2 from, Vector2 to)
            => SegmentDistanceSquared(from, to, A, B) <= Radius * Radius;

        private static float SegmentDistanceSquared(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            if (Intersects(p1, q1, p2, q2))
            {
                return 0f;
            }

            var first = (p1 - CutMath.ClosestPoint(p2, q2, p1)).sqrMagnitude;
            var second = (q1 - CutMath.ClosestPoint(p2, q2, q1)).sqrMagnitude;
            var third = (p2 - CutMath.ClosestPoint(p1, q1, p2)).sqrMagnitude;
            var fourth = (q2 - CutMath.ClosestPoint(p1, q1, q2)).sqrMagnitude;

            return Mathf.Min(Mathf.Min(first, second), Mathf.Min(third, fourth));
        }

        private static bool Intersects(Vector2 p1, Vector2 q1, Vector2 p2, Vector2 q2)
        {
            var d1 = Cross(q1 - p1, p2 - p1);
            var d2 = Cross(q1 - p1, q2 - p1);
            var d3 = Cross(q2 - p2, p1 - p2);
            var d4 = Cross(q2 - p2, q1 - p2);

            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        private static float Cross(Vector2 a, Vector2 b)
            => a.x * b.y - a.y * b.x;
    }
}
