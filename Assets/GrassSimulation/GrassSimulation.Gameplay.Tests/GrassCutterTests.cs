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
        var touchedBeds = new List<int>();
        var from = grid.ToWorld(new Vector2(0.5f, 1f));
        var to = grid.ToWorld(new Vector2(15.5f, 1f));

        grid.Fill(new RectInt(0, 0, 64, 8), PlantKind.Grass);
        var stroke = new CutStroke(from, to, Radius: 0.3f, Tier: 1, CuttingPower: 1f, DeltaTime: 1f / 60f);

        cutter.Cut(stroke, harvested, touchedBeds);

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
        var touchedBeds = new List<int>();
        var point = grid.ToWorld(new Vector2(1f, 1f));
        var stroke = new CutStroke(point, point, Radius: 0.3f, Tier: 1, CuttingPower: 1f, DeltaTime: 1f);

        grid.Fill(new RectInt(0, 0, 8, 8), PlantKind.Grass);
        cutter.Cut(stroke, harvested, touchedBeds);
        var firstCount = harvested.Count;

        harvested.Clear();
        cutter.Cut(stroke, harvested, touchedBeds);

        Assert.That(firstCount, Is.GreaterThan(0));
        Assert.That(harvested, Is.Empty);
    }

    [Test]
    public void LockedPlant_IsNotCutBelowRequiredTier()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var cutter = CreateCutter(grid, toughness: 0.01f);
        var harvested = new List<int>();
        var touchedBeds = new List<int>();
        var point = grid.ToWorld(new Vector2(1f, 1f));

        grid.Fill(new RectInt(0, 0, 8, 8), PlantKind.HardBush);
        cutter.Cut(Stroke(point, tier: 2), harvested, touchedBeds);

        Assert.That(harvested, Is.Empty);

        cutter.Cut(Stroke(point, tier: 3), harvested, touchedBeds);

        Assert.That(harvested, Is.Not.Empty);
    }

    [Test]
    public void ProtectedPlant_ReportsTouchAndIsNeverCut()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var cutter = CreateCutter(grid, toughness: 0.01f);
        var harvested = new List<int>();
        var touchedBeds = new List<int>();
        var point = grid.ToWorld(new Vector2(1f, 1f));

        grid.Fill(new RectInt(0, 0, 8, 8), PlantKind.ProtectedFlower);
        var stroke = new CutStroke(point, point, Radius: 0.3f, Tier: 4, CuttingPower: 10f, DeltaTime: 1f);
        cutter.Cut(stroke, harvested, touchedBeds);

        Assert.That(touchedBeds, Is.EqualTo(new[] { 0 }));
        Assert.That(harvested, Is.Empty);
        Assert.That(cutter.CountCuttableCells(), Is.Zero);
    }

    [Test]
    public void ZoneRadius_ExtendsTheReachBeyondTheBladeRadius()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var point = grid.ToWorld(new Vector2(1f, 1f));
        var cell = grid.IndexOf(4, 4);

        grid.Fill(new RectInt(0, 0, 8, 8), PlantKind.Grass);

        var withoutZone = new List<int>();
        var withZone = new List<int>();
        var stroke = new CutStroke(point, point, Radius: 0.1f, Tier: 1, CuttingPower: 1f, DeltaTime: 1f);

        CreateCutter(grid, toughness: 0.01f).Cut(stroke, withoutZone, new List<int>());
        CreateCutter(grid, toughness: 0.01f, zoneRadius: 0.1f).Cut(stroke, withZone, new List<int>());

        Assert.That(withoutZone, Has.No.Member(cell));
        Assert.That(withZone, Has.Member(cell));
    }

    [Test]
    public void ProtectedPlant_ReportsTheBedItBelongsTo()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var beds = new[] { new RectInt(0, 0, 3, 8), new RectInt(5, 0, 3, 8) };
        var touchedBeds = new List<int>();
        var point = grid.ToWorld(new Vector2(1.6f, 1f));

        grid.Fill(beds[0], PlantKind.ProtectedFlower);
        grid.Fill(beds[1], PlantKind.ProtectedFlower);

        var stroke = new CutStroke(point, point, Radius: 0.2f, Tier: 1, CuttingPower: 1f, DeltaTime: 1f);

        CreateCutter(grid, toughness: 0.01f, beds: beds).Cut(stroke, new List<int>(), touchedBeds);

        Assert.That(touchedBeds, Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void OnePassOverTwoBeds_ReportsEachBedOnce()
    {
        var grid = new FieldGrid(cellsX: 8, cellsZ: 8, CELL_SIZE);
        var beds = new[] { new RectInt(0, 0, 3, 8), new RectInt(5, 0, 3, 8) };
        var touchedBeds = new List<int>();
        var from = grid.ToWorld(new Vector2(0.3f, 1f));
        var to = grid.ToWorld(new Vector2(1.7f, 1f));

        grid.Fill(beds[0], PlantKind.ProtectedFlower);
        grid.Fill(beds[1], PlantKind.ProtectedFlower);

        var cutter = CreateCutter(grid, toughness: 0.01f, beds: beds);
        var stroke = new CutStroke(from, to, Radius: 0.3f, Tier: 1, CuttingPower: 1f, DeltaTime: 1f / 60f);

        cutter.Cut(stroke, new List<int>(), touchedBeds);
        cutter.Cut(stroke, new List<int>(), touchedBeds);

        Assert.That(touchedBeds, Is.EquivalentTo(new[] { 0, 1 }));
        Assert.That(touchedBeds, Has.Count.EqualTo(2));
    }

    private static CutStroke Stroke(Vector3 point, int tier)
        => new(point, point, Radius: 0.3f, Tier: tier, CuttingPower: 1f, DeltaTime: 1f);

    private static GrassCutter CreateCutter(
          FieldGrid grid
        , float toughness
        , float zoneRadius = 0f
        , RectInt[] beds = null
    )
    {
        var plants = new[] {
            new PlantSettings {
                Kind = PlantKind.Grass,
                RequiredTier = 1,
                Toughness = toughness,
                Xp = 1,
                CutZoneRadius = zoneRadius,
            },
            new PlantSettings { Kind = PlantKind.HardBush, RequiredTier = 3, Toughness = toughness, Xp = 4 },
            new PlantSettings { Kind = PlantKind.ProtectedFlower, IsProtected = true, Toughness = 1f },
        };

        return new GrassCutter(grid, new FieldFeedback(grid.Count), plants, beds ?? System.Array.Empty<RectInt>());
    }
}
