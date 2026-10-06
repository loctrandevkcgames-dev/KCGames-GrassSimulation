using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public readonly record struct PlantHarvest(
          int Index
        , PlantKind Kind
        , int Xp
        , int Units
        , int FruitCount
        , Vector3 Position
    );
}
