using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct ObstaclePlacement
    {
        [field: SerializeField]
        public ObstacleKind Kind { get; set; }

        [field: SerializeField]
        public Vector2 Position { get; set; }

        [field: SerializeField]
        public float Yaw { get; set; }

        [field: SerializeField]
        public float Length { get; set; }
    }
}
