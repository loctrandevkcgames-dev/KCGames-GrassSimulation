using System;
using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Editor.Tests;

public sealed class LevelBakerTests
{
    private const string FIELD = "..........\n.gggggggg.\n.gggggggg.\n.ffffff...\n.ffffff...\n..........\n....S.....\n";
    private const string BUSHES = "..........\n.bbbbbbbb.\n.bbbbbbbb.\n.bbbbbbbb.\n..........\n....S.....\n";

    [Test]
    public void Bake_IsDeterministicForTheSameSeed()
    {
        var spec = Spec(grass: 40, flower: 20);
        var first = Bake(spec, FIELD, 10, 7);
        var second = Bake(spec, FIELD, 10, 7);

        Assert.That(second.Kinds, Is.EqualTo(first.Kinds));
        Assert.That(second.Plants, Is.EqualTo(first.Plants));
    }

    [Test]
    public void Bake_IsDeterministicForObjects()
    {
        var first = Bake(Spec(bushLow: 12), BUSHES, 10, 6);
        var second = Bake(Spec(bushLow: 12), BUSHES, 10, 6);

        Assert.That(second.Plants, Is.EqualTo(first.Plants));
    }

    [Test]
    public void Bake_DiffersWhenTheSeedChanges()
    {
        var spec = Spec(grass: 40, flower: 20);
        var first = Bake(spec, "; seed=1\n" + FIELD, 10, 7);
        var second = Bake(spec, "; seed=2\n" + FIELD, 10, 7);

        Assert.That(second.Kinds, Is.Not.EqualTo(first.Kinds));
    }

    [Test]
    public void Bake_PlacesExactlyTheSpecCounts()
    {
        var baked = Bake(Spec(grass: 40, flower: 20), FIELD, 10, 7);

        Assert.That(Count(baked, PlantKind.Grass), Is.EqualTo(40));
        Assert.That(Count(baked, PlantKind.HarvestFlower), Is.EqualTo(20));
    }

    [Test]
    public void Bake_FillsARegionWhenTheCountEqualsItsCapacity()
    {
        var baked = Bake(Spec(grass: 64), FIELD, 10, 7);

        Assert.That(Count(baked, PlantKind.Grass), Is.EqualTo(64));
    }

    [Test]
    public void Bake_FailsWhenTheCountExceedsTheRegion()
    {
        var result = LevelBaker.Bake(Spec(grass: 65), Layout(FIELD, 10, 7), Plants());

        Assert.That(result.TryGetError(out var error), Is.True);
        Assert.That(error.TryGetValue(out LevelBuildError.CapacityExceeded capacity), Is.True);
        Assert.That(capacity.Capacity, Is.EqualTo(64));
    }

    [Test]
    public void Bake_CountsOnlyCellsOutsideTheSpawnClearing()
    {
        var result = LevelBaker.Bake(Spec(grass: 4), Layout("......\ng.....\nS.....\n", 6, 3), Plants());

        Assert.That(result.TryGetError(out var error), Is.True);
        Assert.That(error.TryGetValue(out LevelBuildError.CapacityExceeded capacity), Is.True);
        Assert.That(capacity.Capacity, Is.EqualTo(2));
    }

    [Test]
    public void Bake_PlacesObjectsInsideTheirRegionWithSpacing()
    {
        var baked = Bake(Spec(bushLow: 12), BUSHES, 10, 6);

        Assert.That(baked.Plants.Length, Is.EqualTo(12));

        for (var i = 0; i < baked.Plants.Length; i++)
        {
            var position = baked.Plants[i].Position;

            Assert.That(position.x, Is.InRange(1f, 9f));
            Assert.That(position.y, Is.InRange(2f, 5f));

            for (var j = i + 1; j < baked.Plants.Length; j++)
            {
                Assert.That(Vector2.Distance(position, baked.Plants[j].Position), Is.GreaterThanOrEqualTo(1f));
            }
        }
    }

    [Test]
    public void Bake_FailsWhenTheObjectsDoNotFit()
    {
        var result = LevelBaker.Bake(Spec(bushLow: 40), Layout(BUSHES, 10, 6), Plants());

        Assert.That(result.TryGetError(out var error), Is.True);
        Assert.That(error.TryGetValue(out LevelBuildError.PlacementFailed failed), Is.True);
        Assert.That(failed.Requested, Is.EqualTo(40));
        Assert.That(failed.Placed, Is.LessThan(40));
    }

    [Test]
    public void Bake_AppliesTheFruitOverride()
    {
        var baked = Bake(Spec(tree: 1), "; fruit=T:7\n.......\n.TTTT..\n.TTTT..\n.......\nS......\n", 7, 5);

        Assert.That(baked.Plants[0].FruitCount, Is.EqualTo(7));
    }

    [Test]
    public void Bake_BuildsBedsInCellsAndFillsThemWithProtectedFlowers()
    {
        var baked = Bake(Spec(), "........\n.PP.....\n.PP.....\n........\nS.......\n", 8, 5);

        Assert.That(baked.Beds, Is.EqualTo(new[] { new RectInt(2, 4, 4, 4) }));
        Assert.That(Count(baked, PlantKind.ProtectedFlower), Is.EqualTo(16));
    }

    [Test]
    public void Bake_MergesFenceRunsAndKeepsRocks()
    {
        var baked = Bake(Spec(), "........\n.####...\n.R...#..\n.....#..\nS.......\n", 8, 5);

        Assert.That(baked.Obstacles, Has.Length.EqualTo(3));
        Assert.That(baked.Obstacles[0].Kind, Is.EqualTo(ObstacleKind.Rock));
        Assert.That(baked.Obstacles[1].Length, Is.EqualTo(2f));
        Assert.That(baked.Obstacles[1].Yaw, Is.EqualTo(90f));
        Assert.That(baked.Obstacles[2].Length, Is.EqualTo(4f));
        Assert.That(baked.Obstacles[2].Yaw, Is.EqualTo(0f));
    }

    [Test]
    public void Bake_PlacesTheSpawnAtTheCellCentre()
    {
        var baked = Bake(Spec(), FIELD, 10, 7);

        Assert.That(baked.Spawn, Is.EqualTo(new Vector2(4.5f, 0.5f)));
    }

    private static BakedLevel Bake(LevelSpec spec, string text, int width, int height)
    {
        var result = LevelBaker.Bake(spec, Layout(text, width, height), Plants());

        Assert.That(result.TryGetValue(out var baked), Is.True);
        return baked;
    }

    private static LayoutText Layout(string text, int width, int height)
    {
        var result = LayoutText.Parse("T", text, width, height);

        Assert.That(result.TryGetValue(out var layout), Is.True);
        return layout;
    }

    private static int Count(BakedLevel baked, PlantKind kind)
        => Array.FindAll(baked.Kinds, value => value == (byte)kind).Length;

    private static PlantDefinition[] Plants()
    {
        return new[] {
            new PlantDefinition { Kind = PlantKind.BushLow, CutZoneRadius = 0.4f },
            new PlantDefinition { Kind = PlantKind.FruitTree, CutZoneRadius = 0.6f },
        };
    }

    private static LevelSpec Spec(int grass = 0, int flower = 0, int bushLow = 0, int tree = 0)
    {
        var counts = new int[PlantKindExtensions.Length];

        counts[(int)PlantKind.Grass] = grass;
        counts[(int)PlantKind.HarvestFlower] = flower;
        counts[(int)PlantKind.BushLow] = bushLow;
        counts[(int)PlantKind.FruitTree] = tree;

        return new LevelSpec(
              Id: "T01"
            , Order: 1
            , Step: 1
            , Name: "Test"
            , Decision: "Test"
            , Type: LevelType.Normal
            , Width: 10
            , Length: 7
            , ZoneCount: 1
            , BedCount: 0
            , MaxTier: 1
            , PlantCounts: counts
            , Quotas: Array.Empty<QuotaSettings>()
            , IsTimed: false
            , SuggestedTimer: 0f
            , Unlock: default
        );
    }
}
