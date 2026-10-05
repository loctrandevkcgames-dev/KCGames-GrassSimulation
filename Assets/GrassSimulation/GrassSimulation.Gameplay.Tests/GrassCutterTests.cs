using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class GrassCutterTests
{
    private const float CELL_SIZE = 0.25f;

    [Test]
    public void FastStroke_LeavesNoUncutStripeAlongThePath()
    {
        var grid = new FieldGrid(cellsX: 64, cellsZ: 8, CELL_SIZE);
        var cutter = CreateCutter(grid, toughness: 1e-4f);
        var harvested = new List<int>();
        var from = grid.ToWorld(new Vector2(0.5f, 1f));
        var to = grid.ToWorld(new Vector2(15.5f, 1f));

        grid.Fill(new RectInt(0, 0, 64, 8), PlantKind.Grass);
        cutter.Cut(new CutStroke(from, to, Radius: 0.3f, Tier: 1, CuttingPower: 1f, DeltaTime: 1f / 60f), harvested);

        for (var x = 2; x < 62; x++)
        {
            Assert.That(grid.GetProgress(grid.IndexOf(x, 4)), Is.EqualTo(1f), $"cell {x} on the path was skipped");
        }
    }

    [Test]
    public void CutCell_IsHarvestedOnlyOnce()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var cutter = CreateCutter(grid, toughness: 0.01f);
        var harvested = new List<int>();
        var point = grid.ToWorld(new Vector2(1f, 1f));
        var stroke = new CutStroke(point, point, Radius: 0.3f, Tier: 1, CuttingPower: 1f, DeltaTime: 1f);

        grid.Fill(new RectInt(0, 0, 8, 8), PlantKind.Grass);
        cutter.Cut(stroke, harvested);
        var firstCount = harvested.Count;

        harvested.Clear();
        cutter.Cut(stroke, harvested);

        Assert.That(firstCount, Is.GreaterThan(0));
        Assert.That(harvested, Is.Empty);
    }

    [Test]
    public void LockedPlant_IsNotCutBelowRequiredTier()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var cutter = CreateCutter(grid, toughness: 0.01f);
        var harvested = new List<int>();
        var point = grid.ToWorld(new Vector2(1f, 1f));

        grid.Fill(new RectInt(0, 0, 8, 8), PlantKind.HardBush);
        cutter.Cut(new CutStroke(point, point, Radius: 0.3f, Tier: 2, CuttingPower: 1f, DeltaTime: 1f), harvested);

        Assert.That(harvested, Is.Empty);

        cutter.Cut(new CutStroke(point, point, Radius: 0.3f, Tier: 3, CuttingPower: 1f, DeltaTime: 1f), harvested);

        Assert.That(harvested, Is.Not.Empty);
    }

    [Test]
    public void ProtectedPlant_ReportsTouchAndIsNeverCut()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var cutter = CreateCutter(grid, toughness: 0.01f);
        var harvested = new List<int>();
        var point = grid.ToWorld(new Vector2(1f, 1f));

        grid.Fill(new RectInt(0, 0, 8, 8), PlantKind.ProtectedFlower);
        var stroke = new CutStroke(point, point, Radius: 0.3f, Tier: 4, CuttingPower: 10f, DeltaTime: 1f);
        var touchedProtected = cutter.Cut(stroke, harvested);

        Assert.That(touchedProtected, Is.True);
        Assert.That(harvested, Is.Empty);
        Assert.That(cutter.CountCuttableCells(), Is.Zero);
    }

    private static GrassCutter CreateCutter(FieldGrid grid, float toughness)
    {
        var plants = new[] {
            new PlantSettings { Kind = PlantKind.Grass, RequiredTier = 1, Toughness = toughness, Xp = 1 },
            new PlantSettings { Kind = PlantKind.HardBush, RequiredTier = 3, Toughness = toughness, Xp = 4 },
            new PlantSettings { Kind = PlantKind.ProtectedFlower, IsProtected = true, Toughness = 1f },
        };

        return new GrassCutter(grid, new FieldFeedback(grid.Count), plants);
    }
}
