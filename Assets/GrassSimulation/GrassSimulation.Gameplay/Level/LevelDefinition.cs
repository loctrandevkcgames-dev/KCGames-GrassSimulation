using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "Grass Simulation/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField]
        private string _id = "level-00";

        [SerializeField]
        private int _cellsX = 36;

        [SerializeField]
        private int _cellsZ = 28;

        [SerializeField]
        private float _cellSize = 0.5f;

        [SerializeField]
        private int _seed;

        [SerializeField]
        private PlantKind _baseKind = PlantKind.Grass;

        [SerializeField]
        private LevelZone[] _zones = Array.Empty<LevelZone>();

        [SerializeField]
        private Vector2 _spawn = new(2f, 2f);

        [SerializeField]
        private float _spawnClearing = 1f;

        [SerializeField]
        private byte[] _bakedKinds = Array.Empty<byte>();

        [SerializeField]
        private PlantPlacement[] _plants = Array.Empty<PlantPlacement>();

        [SerializeField]
        private ObstaclePlacement[] _obstacles = Array.Empty<ObstaclePlacement>();

        [SerializeField]
        private float _timeLimit = 120f;

        [SerializeField]
        private QuotaSettings[] _quotas = Array.Empty<QuotaSettings>();

        [SerializeField]
        private LevelType _type = LevelType.Normal;

        [SerializeField]
        private int _maxTier = 1;

        [NonSerialized]
        private RectInt[] _beds;

        public LevelId Id => new(_id);

        public int CellsX => _cellsX;

        public int CellsZ => _cellsZ;

        public float CellSize => _cellSize;

        public int Seed => _seed;

        public Vector2 Spawn => _spawn;

        public ReadOnlySpan<PlantPlacement> Plants => _plants;

        public ReadOnlySpan<ObstaclePlacement> Obstacles => _obstacles;

        public float TimeLimit => _timeLimit;

        public ReadOnlySpan<QuotaSettings> Quotas => _quotas;

        public LevelType Type => _type;

        public int MaxTier => _maxTier;

        public ReadOnlySpan<RectInt> Beds => _beds ??= CollectBeds();

        public FieldGrid CreateGrid()
        {
            var grid = new FieldGrid(_cellsX, _cellsZ, _cellSize);

            if (_bakedKinds.Length == grid.Count)
            {
                ApplyBakedKinds(grid);
            }
            else
            {
                ApplyZones(grid);
            }

            grid.ClearCircle(_spawn, _spawnClearing);
            ClearUnderObstacles(grid);
            return grid;
        }

        public ObstacleField CreateObstacles(FieldGrid grid)
        {
            var obstacles = new ObstacleField();
            var count = _obstacles.Length;

            for (var i = 0; i < count; i++)
            {
                obstacles.Add(ObstacleShape.From(in _obstacles[i], grid.Origin));
            }

            return obstacles;
        }

        public PlantObjectField CreatePlantObjects(FieldGrid grid, ReadOnlySpan<PlantDefinition> plants)
        {
            var objects = new PlantObjectField(plants);
            var count = _plants.Length;

            for (var i = 0; i < count; i++)
            {
                var placement = _plants[i];

                objects.Add(placement.Kind, placement.Position + grid.Origin, placement.FruitCount);
            }

            return objects;
        }

        private void ApplyBakedKinds(FieldGrid grid)
        {
            var count = _bakedKinds.Length;

            for (var i = 0; i < count; i++)
            {
                grid.SetKind(i, (PlantKind)_bakedKinds[i]);
            }
        }

        private void ApplyZones(FieldGrid grid)
        {
            grid.Fill(new RectInt(0, 0, _cellsX, _cellsZ), _baseKind);

            var zoneCount = _zones.Length;

            for (var i = 0; i < zoneCount; i++)
            {
                var zone = _zones[i];
                grid.Fill(zone.Cells, zone.Kind);
            }
        }

        private void ClearUnderObstacles(FieldGrid grid)
        {
            var count = _obstacles.Length;

            for (var i = 0; i < count; i++)
            {
                var shape = ObstacleShape.From(in _obstacles[i], Vector2.zero);

                grid.ClearCapsule(shape.A, shape.B, shape.Radius);
            }
        }

        private RectInt[] CollectBeds()
        {
            var beds = new List<RectInt>();
            var zoneCount = _zones.Length;

            for (var i = 0; i < zoneCount; i++)
            {
                var zone = _zones[i];

                if (zone.Kind == PlantKind.ProtectedFlower)
                {
                    beds.Add(zone.Cells);
                }
            }

            return beds.ToArray();
        }

        private void OnValidate()
        {
            _beds = null;
        }
    }
}
