using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed record class LevelLayout(
          int CellsX
        , int CellsZ
        , float CellSize
        , PlantKind[] Kinds
        , PlantPlacement[] Plants
        , ObstaclePlacement[] Obstacles
        , RectInt[] Beds
        , Vector2 Spawn
        , int MaxTier
        , LevelType Type
        , float TimeLimit
        , QuotaSettings[] Quotas
    )
    {
        public Vector2 Size => new(CellsX * CellSize, CellsZ * CellSize);

        public static LevelLayout From(LevelDefinition level)
        {
            var grid = level.CreateGrid();
            var kinds = new PlantKind[grid.Count];

            for (var i = 0; i < kinds.Length; i++)
            {
                kinds[i] = grid.GetKind(i);
            }

            return new LevelLayout(
                  level.CellsX
                , level.CellsZ
                , level.CellSize
                , kinds
                , level.Plants.ToArray()
                , level.Obstacles.ToArray()
                , level.Beds.ToArray()
                , level.Spawn
                , level.MaxTier
                , level.Type
                , level.TimeLimit
                , level.Quotas.ToArray()
            );
        }
    }
}
