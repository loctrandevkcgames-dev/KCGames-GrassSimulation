using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public struct PlantObjectState
    {
        public PlantKind Kind;
        public Vector2 Position;
        public float Radius;
        public float Progress;
        public bool Harvested;
        public int FruitCount;
    }
}
