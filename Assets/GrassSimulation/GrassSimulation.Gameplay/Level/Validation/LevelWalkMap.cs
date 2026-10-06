using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    internal sealed class LevelWalkMap
    {
        private const float EPSILON = 1e-4f;

        private readonly ObstacleShape[] _shapes;
        private readonly bool[] _reached;
        private readonly float _step;
        private readonly int _countX;
        private readonly int _countZ;

        public LevelWalkMap(LevelLayout layout, float bodyRadius, float step, float bedInflate)
        {
            _step = step;
            _countX = Mathf.RoundToInt(layout.Size.x / step) + 1;
            _countZ = Mathf.RoundToInt(layout.Size.y / step) + 1;
            _reached = new bool[_countX * _countZ];
            _shapes = BuildShapes(layout);

            var free = BuildFree(layout, bodyRadius, bedInflate);
            var spawnX = Mathf.RoundToInt(layout.Spawn.x / step);
            var spawnZ = Mathf.RoundToInt(layout.Spawn.y / step);

            IsSpawnFree = spawnX >= 0 && spawnX < _countX && spawnZ >= 0 && spawnZ < _countZ
                && free[spawnZ * _countX + spawnX];

            if (IsSpawnFree)
            {
                Flood(free, spawnZ * _countX + spawnX);
            }
        }

        public bool IsSpawnFree { get; }

        public bool CanCut(Vector2 target, float reach)
        {
            var xMin = Mathf.Max(0, Mathf.CeilToInt((target.x - reach) / _step - EPSILON));
            var xMax = Mathf.Min(_countX - 1, Mathf.FloorToInt((target.x + reach) / _step + EPSILON));
            var zMin = Mathf.Max(0, Mathf.CeilToInt((target.y - reach) / _step - EPSILON));
            var zMax = Mathf.Min(_countZ - 1, Mathf.FloorToInt((target.y + reach) / _step + EPSILON));
            var reachSquared = reach * reach + EPSILON;

            for (var z = zMin; z <= zMax; z++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    if (_reached[z * _countX + x] == false)
                    {
                        continue;
                    }

                    var point = new Vector2(x * _step, z * _step);

                    if ((point - target).sqrMagnitude <= reachSquared && IsOccluded(point, target) == false)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ObstacleShape[] BuildShapes(LevelLayout layout)
        {
            var shapes = new ObstacleShape[layout.Obstacles.Length];

            for (var i = 0; i < shapes.Length; i++)
            {
                shapes[i] = ObstacleShape.From(in layout.Obstacles[i], Vector2.zero);
            }

            return shapes;
        }

        private static float DistanceToRect(Vector2 point, Rect rect)
        {
            var nearest = Vector2.Max(rect.min, Vector2.Min(point, rect.max));

            return Vector2.Distance(point, nearest);
        }

        private bool[] BuildFree(LevelLayout layout, float bodyRadius, float bedInflate)
        {
            var free = new bool[_countX * _countZ];
            var size = layout.Size;
            var beds = BuildBedRects(layout, bedInflate);

            for (var z = 0; z < _countZ; z++)
            {
                for (var x = 0; x < _countX; x++)
                {
                    var point = new Vector2(x * _step, z * _step);

                    free[z * _countX + x] = IsInsideBorder(point, size, bodyRadius)
                        && IsClearOfObstacles(point, bodyRadius)
                        && IsClearOfBeds(point, beds, bedInflate);
                }
            }

            return free;
        }

        private static bool IsInsideBorder(Vector2 point, Vector2 size, float bodyRadius)
        {
            return point.x >= bodyRadius - EPSILON
                && point.y >= bodyRadius - EPSILON
                && point.x <= size.x - bodyRadius + EPSILON
                && point.y <= size.y - bodyRadius + EPSILON;
        }

        private static Rect[] BuildBedRects(LevelLayout layout, float bedInflate)
        {
            if (bedInflate <= 0f)
            {
                return Array.Empty<Rect>();
            }

            var rects = new Rect[layout.Beds.Length];

            for (var i = 0; i < rects.Length; i++)
            {
                var bed = layout.Beds[i];

                rects[i] = Rect.MinMaxRect(
                      bed.xMin * layout.CellSize
                    , bed.yMin * layout.CellSize
                    , bed.xMax * layout.CellSize
                    , bed.yMax * layout.CellSize
                );
            }

            return rects;
        }

        private bool IsClearOfObstacles(Vector2 point, float bodyRadius)
        {
            for (var i = 0; i < _shapes.Length; i++)
            {
                var shape = _shapes[i];

                if (Vector2.Distance(point, shape.Closest(point)) < shape.Radius + bodyRadius - EPSILON)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsClearOfBeds(Vector2 point, Rect[] beds, float bedInflate)
        {
            for (var i = 0; i < beds.Length; i++)
            {
                if (DistanceToRect(point, beds[i]) < bedInflate - EPSILON)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsOccluded(Vector2 from, Vector2 to)
        {
            for (var i = 0; i < _shapes.Length; i++)
            {
                if (_shapes[i].IsBlocking(from, to))
                {
                    return true;
                }
            }

            return false;
        }

        private void Flood(bool[] free, int start)
        {
            var queue = new Queue<int>();

            _reached[start] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % _countX;
                var z = index / _countX;

                Visit(free, queue, x - 1, z);
                Visit(free, queue, x + 1, z);
                Visit(free, queue, x, z - 1);
                Visit(free, queue, x, z + 1);
            }
        }

        private void Visit(bool[] free, Queue<int> queue, int x, int z)
        {
            if (x < 0 || x >= _countX || z < 0 || z >= _countZ)
            {
                return;
            }

            var index = z * _countX + x;

            if (free[index] == false || _reached[index])
            {
                return;
            }

            _reached[index] = true;
            queue.Enqueue(index);
        }
    }
}
