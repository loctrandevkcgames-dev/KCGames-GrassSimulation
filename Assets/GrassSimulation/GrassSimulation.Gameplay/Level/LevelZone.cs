using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct LevelZone
    {
        [field: SerializeField]
        public PlantKind Kind { get; set; }

        [field: SerializeField]
        public RectInt Cells { get; set; }
    }
}
