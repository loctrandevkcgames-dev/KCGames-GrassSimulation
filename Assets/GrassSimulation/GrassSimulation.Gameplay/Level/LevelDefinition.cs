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
        private int _cellsX = 72;

        [SerializeField]
        private int _cellsZ = 56;

        [SerializeField]
        private float _cellSize = 0.25f;

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
        private GameObject _propLayout;

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

        public GameObject PropLayout => _propLayout;

        public float TimeLimit => _timeLimit;

        public ReadOnlySpan<QuotaSettings> Quotas => _quotas;

        public LevelType Type => _type;

        public int MaxTier => _maxTier;

        public ReadOnlySpan<RectInt> Beds => _beds ??= CollectBeds();

        public FieldGrid CreateGrid()
        {
            var grid = new FieldGrid(_cellsX, _cellsZ, _cellSize);
            grid.Fill(new RectInt(0, 0, _cellsX, _cellsZ), _baseKind);

            var zoneCount = _zones.Length;

            for (var i = 0; i < zoneCount; i++)
            {
                var zone = _zones[i];
                grid.Fill(zone.Cells, zone.Kind);
            }

            grid.ClearCircle(_spawn, _spawnClearing);
            return grid;
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
