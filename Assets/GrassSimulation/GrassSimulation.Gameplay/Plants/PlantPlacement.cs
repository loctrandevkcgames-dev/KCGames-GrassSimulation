using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct PlantPlacement
    {
        [field: SerializeField]
        public PlantKind Kind { get; set; }

        [field: SerializeField]
        public Vector2 Position { get; set; }

        [field: SerializeField]
        public int FruitCount { get; set; }

        [field: SerializeField]
        public float Yaw { get; set; }

        [field: SerializeField]
        public float Scale { get; set; }
    }
}
