using System;
using System.Linq;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

internal static class LevelValidatorFixture
{
    public const int CELLS = 16;
    public const float CELL_SIZE = 0.5f;

    public static PlantDefinition[] Plants()
    {
        return new[] {
            Plant(PlantKind.Grass, tier: 1, xp: 1, zone: 0.25f),
            Plant(PlantKind.HarvestFlower, tier: 1, xp: 1, zone: 0.25f),
            Plant(PlantKind.ThickGrass, tier: 2, xp: 2, zone: 0.25f),
            Plant(PlantKind.BushLow, tier: 2, xp: 3, zone: 0.4f),
            Plant(PlantKind.FruitTree, tier: 4, xp: 0, zone: 0.6f),
            new PlantDefinition { Kind = PlantKind.ProtectedFlower, IsProtected = true, RequiredTier = 99 },
        };
    }

    public static LevelLayout Layout()
    {
        var kinds = new PlantKind[CELLS * CELLS];

        Fill(kinds, new RectInt(2, 2, 8, 4), PlantKind.Grass);
        Fill(kinds, new RectInt(2, 10, 6, 4), PlantKind.HarvestFlower);

        return new LevelLayout(
              CellsX: CELLS
            , CellsZ: CELLS
            , CellSize: CELL_SIZE
            , Kinds: kinds
            , Plants: Array.Empty<PlantPlacement>()
            , Obstacles: Array.Empty<ObstaclePlacement>()
            , Beds: Array.Empty<RectInt>()
            , Spawn: new Vector2(6.5f, 0.5f)
            , MaxTier: 1
            , Type: LevelType.Normal
            , TimeLimit: 60f
            , Quotas: new[] { Quota(PlantKind.HarvestFlower, 20) }
        );
    }

    public static LevelSpec Spec(LevelLayout layout, int order = 1)
    {
        var counts = new int[PlantKindExtensions.Length];

        for (var i = 0; i < layout.Kinds.Length; i++)
        {
            counts[(int)layout.Kinds[i]]++;
        }

        for (var i = 0; i < layout.Plants.Length; i++)
        {
            counts[(int)layout.Plants[i].Kind]++;
        }

        counts[(int)PlantKind.None] = 0;
        counts[(int)PlantKind.ProtectedFlower] = 0;

        return new LevelSpec(
              Id: "T01"
            , Order: order
            , Step: 1
            , Name: "Test"
            , Decision: "Test"
            , Type: layout.Type
            , Width: layout.CellsX / 2
            , Length: layout.CellsZ / 2
            , ZoneCount: 2
            , BedCount: layout.Beds.Length
            , MaxTier: layout.MaxTier
            , PlantCounts: counts
            , Quotas: layout.Quotas.ToArray()
            , IsTimed: layout.TimeLimit > 0f
            , SuggestedTimer: layout.TimeLimit
            , Unlock: default
        );
    }

    public static PlantDefinition Plant(PlantKind kind, int tier, int xp, float zone)
        => new() { Kind = kind, RequiredTier = tier, Xp = xp, CutZoneRadius = zone };

    public static QuotaSettings Quota(PlantKind kind, int amount, bool isBonus = false)
        => new() { Kind = kind, Amount = amount, IsBonus = isBonus };

    public static void Fill(PlantKind[] kinds, RectInt rect, PlantKind kind)
    {
        for (var z = rect.yMin; z < rect.yMax; z++)
        {
            for (var x = rect.xMin; x < rect.xMax; x++)
            {
                kinds[z * CELLS + x] = kind;
            }
        }
    }

    public static ObstaclePlacement Rock(float x, float z, float diameter = 1f)
        => new() { Kind = ObstacleKind.Rock, Position = new Vector2(x, z), Length = diameter };

    public static ObstaclePlacement Fence(float x, float z, float length, float yaw = 0f)
        => new() { Kind = ObstacleKind.Fence, Position = new Vector2(x, z), Length = length, Yaw = yaw };
}
