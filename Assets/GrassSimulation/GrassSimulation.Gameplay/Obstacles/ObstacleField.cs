using System.Collections.Generic;
using EncosyTower.Collections;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class ObstacleField : IHasCount, IClearable
    {
        private const int SLIDE_PASSES = 3;
        private const float MIN_DISTANCE = 1e-4f;

        private readonly List<ObstacleShape> _shapes = new();

        public int Count => _shapes.Count;

        public ObstacleShape this[int index] => _shapes[index];

        public void Add(in ObstacleShape shape)
        {
            _shapes.Add(shape);
        }

        public void Clear()
        {
            _shapes.Clear();
        }

        public Vector2 Resolve(Vector2 position, float bodyRadius, ref Vector2 velocity)
        {
            var count = _shapes.Count;

            for (var pass = 0; pass < SLIDE_PASSES; pass++)
            {
                var isPushed = false;

                for (var i = 0; i < count; i++)
                {
                    var shape = _shapes[i];
                    var closest = shape.Closest(position);
                    var offset = position - closest;
                    var minDistance = shape.Radius + bodyRadius;
                    var distance = offset.magnitude;

                    if (distance >= minDistance)
                    {
                        continue;
                    }

                    var normal = distance > MIN_DISTANCE ? offset / distance : FallbackNormal(shape, velocity);

                    position = closest + normal * minDistance;
                    velocity = RemoveInward(velocity, normal);
                    isPushed = true;
                }

                if (isPushed == false)
                {
                    break;
                }
            }

            return position;
        }

        public bool HasObstacleNear(Vector2 min, Vector2 max)
        {
            var count = _shapes.Count;

            for (var i = 0; i < count; i++)
            {
                if (_shapes[i].Overlaps(min, max))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsOccluded(Vector2 from, Vector2 to)
        {
            var count = _shapes.Count;

            for (var i = 0; i < count; i++)
            {
                if (_shapes[i].IsBlocking(from, to))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 RemoveInward(Vector2 velocity, Vector2 normal)
        {
            var inward = Vector2.Dot(velocity, normal);

            return inward < 0f ? velocity - normal * inward : velocity;
        }

        private static Vector2 FallbackNormal(in ObstacleShape shape, Vector2 velocity)
        {
            var axis = shape.B - shape.A;
            var normal = axis.sqrMagnitude > MIN_DISTANCE ? new Vector2(-axis.y, axis.x).normalized : Vector2.right;

            return Vector2.Dot(normal, velocity) > 0f ? -normal : normal;
        }
    }
}
