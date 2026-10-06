using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.Editor
{
    public sealed record class BakedLevel(
          int Seed
        , int CellsX
        , int CellsZ
        , float CellSize
        , byte[] Kinds
        , PlantPlacement[] Plants
        , ObstaclePlacement[] Obstacles
        , RectInt[] Beds
        , Vector2 Spawn
        , float SpawnClearing
    );
}
