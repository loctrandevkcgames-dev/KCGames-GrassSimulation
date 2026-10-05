using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class FieldGrid
    {
        private readonly PlantKind[] _kinds;
        private readonly float[] _progress;

        public FieldGrid(int cellsX, int cellsZ, float cellSize)
        {
            CellsX = cellsX;
            CellsZ = cellsZ;
            CellSize = cellSize;
            Origin = new Vector2(-cellsX * cellSize * 0.5f, -cellsZ * cellSize * 0.5f);
            _kinds = new PlantKind[cellsX * cellsZ];
            _progress = new float[cellsX * cellsZ];
        }

        public int CellsX { get; }

        public int CellsZ { get; }

        public float CellSize { get; }

        public Vector2 Origin { get; }

        public int Count => _kinds.Length;

        public Vector2 Size => new(CellsX * CellSize, CellsZ * CellSize);

        public Rect Bounds => new(Origin, Size);

        public int IndexOf(int x, int z)
            => z * CellsX + x;

        public PlantKind GetKind(int index)
            => _kinds[index];

        public float GetProgress(int index)
            => _progress[index];

        public void SetProgress(int index, float progress)
        {
            _progress[index] = progress;
        }

        public Vector2 CellCenter(int x, int z)
            => new((x + 0.5f) * CellSize, (z + 0.5f) * CellSize);

        public Vector2 CellCenter(int index)
            => CellCenter(index % CellsX, index / CellsX);

        public Vector2 ToLocal(Vector3 world)
            => new Vector2(world.x, world.z) - Origin;

        public Vector3 ToWorld(Vector2 local)
        {
            var world = local + Origin;
            return new Vector3(world.x, 0f, world.y);
        }

        public void Fill(RectInt rect, PlantKind kind)
        {
            for (var z = rect.yMin; z < rect.yMax; z++)
            {
                for (var x = rect.xMin; x < rect.xMax; x++)
                {
                    _kinds[IndexOf(x, z)] = kind;
                }
            }
        }

        public void ClearCircle(Vector2 center, float radius)
        {
            var extent = Vector2.one * radius;
            GetCellRange(center - extent, center + extent, out var xMin, out var zMin, out var xMax, out var zMax);

            for (var z = zMin; z <= zMax; z++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    if (Vector2.Distance(CellCenter(x, z), center) <= radius)
                    {
                        _kinds[IndexOf(x, z)] = PlantKind.None;
                    }
                }
            }
        }

        public void GetCellRange(Vector2 min, Vector2 max, out int xMin, out int zMin, out int xMax, out int zMax)
        {
            xMin = Mathf.Max(0, Mathf.FloorToInt(min.x / CellSize));
            zMin = Mathf.Max(0, Mathf.FloorToInt(min.y / CellSize));
            xMax = Mathf.Min(CellsX - 1, Mathf.FloorToInt(max.x / CellSize));
            zMax = Mathf.Min(CellsZ - 1, Mathf.FloorToInt(max.y / CellSize));
        }

        public void ResetProgress()
        {
            Array.Clear(_progress, 0, _progress.Length);
        }
    }
}
