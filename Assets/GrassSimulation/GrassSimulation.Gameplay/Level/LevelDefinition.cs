using System;
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
        private bool _failOnProtectedHits;

        [SerializeField]
        private int _protectedHitLimit = 3;

        [SerializeField]
        private float _protectedHitCooldown = 1f;

        public string Id => _id;

        public int CellsX => _cellsX;

        public int CellsZ => _cellsZ;

        public float CellSize => _cellSize;

        public int Seed => _seed;

        public Vector2 Spawn => _spawn;

        public GameObject PropLayout => _propLayout;

        public float TimeLimit => _timeLimit;

        public ReadOnlySpan<QuotaSettings> Quotas => _quotas;

        public bool FailOnProtectedHits => _failOnProtectedHits;

        public int ProtectedHitLimit => _protectedHitLimit;

        public float ProtectedHitCooldown => _protectedHitCooldown;

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

        public bool TryGetProtectedBed(out RectInt cells)
        {
            var zoneCount = _zones.Length;

            for (var i = 0; i < zoneCount; i++)
            {
                var zone = _zones[i];

                if (zone.Kind == PlantKind.ProtectedFlower)
                {
                    cells = zone.Cells;
                    return true;
                }
            }

            cells = default;
            return false;
        }
    }
}
