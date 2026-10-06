using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class PlantObjectFieldTests
{
    private const float BLADE_RADIUS = 0.65f;

    private readonly List<PlantHarvest> _harvested = new();
    private readonly PlantContactReport _contacts = new();

    [SetUp]
    public void SetUp()
    {
        _harvested.Clear();
        _contacts.Clear();
    }

    [Test]
    public void Melon_NeedsTier3()
    {
        var field = CreateField();

        field.Add(PlantKind.Melon, Vector2.zero, fruitCount: 0);
        field.Cut(Stroke(Vector2.zero, tier: 2), obstacles: null, _harvested, _contacts);

        Assert.That(_harvested, Is.Empty);
        Assert.That(_contacts.Locked, Has.Count.EqualTo(1));
        Assert.That(_contacts.Locked[0].Kind, Is.EqualTo(PlantKind.Melon));
        Assert.That(_contacts.Locked[0].RequiredTier, Is.EqualTo(3));
        Assert.That(field.Get(0).Progress, Is.Zero);

        field.Cut(Stroke(Vector2.zero, tier: 3), obstacles: null, _harvested, _contacts);

        Assert.That(_harvested, Has.Count.EqualTo(1));
    }

    [Test]
    public void CompletedObject_IsAwardedOnlyOnce()
    {
        var field = CreateField();

        field.Add(PlantKind.Vegetable, Vector2.zero, fruitCount: 0);
        field.Cut(Stroke(Vector2.zero, tier: 2), obstacles: null, _harvested, _contacts);
        field.Cut(Stroke(Vector2.zero, tier: 2), obstacles: null, _harvested, _contacts);
        field.Cut(Stroke(Vector2.zero, tier: 2), obstacles: null, _harvested, _contacts);

        Assert.That(_harvested, Has.Count.EqualTo(1));
        Assert.That(_harvested[0].Xp, Is.EqualTo(4));
        Assert.That(_harvested[0].Units, Is.EqualTo(1));
        Assert.That(field.Get(0).Harvested, Is.True);
        Assert.That(field.CountHarvested(PlantKind.Vegetable), Is.EqualTo(1));
    }

    [Test]
    public void Reach_IsTheBladeRadiusPlusTheZoneRadius()
    {
        var field = CreateField();
        var zoneRadius = field.GetPlant(PlantKind.Vegetable).CutZoneRadius;
        var inside = new Vector2(BLADE_RADIUS + zoneRadius - 0.02f, 0f);
        var outside = new Vector2(BLADE_RADIUS + zoneRadius + 0.02f, 0f);

        field.Add(PlantKind.Vegetable, inside, fruitCount: 0);
        field.Add(PlantKind.Vegetable, outside, fruitCount: 0);
        field.Cut(Stroke(Vector2.zero, tier: 2), obstacles: null, _harvested, _contacts);

        Assert.That(_harvested, Has.Count.EqualTo(1));
        Assert.That(_harvested[0].Index, Is.Zero);
    }

    [Test]
    public void FruitTree_UsesTheCatalogFruitCountUnlessThePlacementOverridesIt()
    {
        var field = CreateField();

        field.Add(PlantKind.FruitTree, Vector2.zero, fruitCount: 0);
        field.Add(PlantKind.FruitTree, new Vector2(0f, 0.1f), fruitCount: 7);
        field.Cut(Stroke(Vector2.zero, tier: 4, deltaTime: 10f), obstacles: null, _harvested, _contacts);

        Assert.That(_harvested, Has.Count.EqualTo(2));
        Assert.That(_harvested[0].FruitCount, Is.EqualTo(5));
        Assert.That(_harvested[1].FruitCount, Is.EqualTo(7));
        Assert.That(_harvested[0].Units, Is.EqualTo(1));
    }

    [Test]
    public void Tier4Plants_GiveNoXp()
    {
        var field = CreateField();

        field.Add(PlantKind.FruitTree, Vector2.zero, fruitCount: 0);
        field.Add(PlantKind.GiantFruit, new Vector2(0f, 0.2f), fruitCount: 0);
        field.Cut(Stroke(Vector2.zero, tier: 4, deltaTime: 10f), obstacles: null, _harvested, _contacts);

        Assert.That(_harvested, Has.Count.EqualTo(2));
        Assert.That(_harvested[0].Xp, Is.Zero);
        Assert.That(_harvested[1].Xp, Is.Zero);
    }

    [Test]
    public void PartialProgress_PersistsBetweenStrokes()
    {
        var field = CreateField();

        field.Add(PlantKind.Melon, Vector2.zero, fruitCount: 0);
        field.Cut(Stroke(Vector2.zero, tier: 3, deltaTime: 0.4f), obstacles: null, _harvested, _contacts);

        var firstProgress = field.Get(0).Progress;

        field.Cut(Stroke(new Vector2(5f, 0f), tier: 3, deltaTime: 0.4f), obstacles: null, _harvested, _contacts);

        Assert.That(firstProgress, Is.EqualTo(0.4f / 0.9f).Within(0.001f));
        Assert.That(field.Get(0).Progress, Is.EqualTo(firstProgress));
        Assert.That(_harvested, Is.Empty);

        field.Cut(Stroke(Vector2.zero, tier: 3, deltaTime: 0.4f), obstacles: null, _harvested, _contacts);

        Assert.That(field.Get(0).Progress, Is.EqualTo(0.8f / 0.9f).Within(0.001f));
    }

    [Test]
    public void ResetProgress_RestoresEveryObject()
    {
        var field = CreateField();

        field.Add(PlantKind.Vegetable, Vector2.zero, fruitCount: 0);
        field.Cut(Stroke(Vector2.zero, tier: 2, deltaTime: 10f), obstacles: null, _harvested, _contacts);
        field.ResetProgress();

        Assert.That(field.Get(0).Harvested, Is.False);
        Assert.That(field.Get(0).Progress, Is.Zero);
    }

    [Test]
    public void SweptStroke_CutsAnObjectBetweenItsEnds()
    {
        var field = CreateField();
        var stroke = new CutStroke(
              new Vector3(-5f, 0f, 0f)
            , new Vector3(5f, 0f, 0f)
            , BLADE_RADIUS
            , Tier: 2
            , CuttingPower: 1f
            , DeltaTime: 10f
        );

        field.Add(PlantKind.Vegetable, Vector2.zero, fruitCount: 0);
        field.Cut(stroke, obstacles: null, _harvested, _contacts);

        Assert.That(_harvested, Has.Count.EqualTo(1));
    }

    [Test]
    public void OccludedObject_IsNotCut()
    {
        var field = CreateField();
        var obstacles = new ObstacleField();
        var fence = ObstacleShape.Capsule(ObstacleKind.Fence, new Vector2(0.4f, -2f), new Vector2(0.4f, 2f), 0.12f);

        obstacles.Add(in fence);
        field.Add(PlantKind.Vegetable, new Vector2(0.8f, 0f), fruitCount: 0);

        var stroke = Stroke(Vector2.zero, tier: 2, deltaTime: 10f);

        field.Cut(stroke, obstacles, _harvested, _contacts);

        Assert.That(_harvested, Is.Empty);
        Assert.That(field.Get(0).Progress, Is.Zero);
    }

    [Test]
    public void FastPassOverATougherPlant_ReportsASlowHint()
    {
        var field = CreateField();
        var stroke = new CutStroke(
              new Vector3(-0.04f, 0f, 0f)
            , new Vector3(0f, 0f, 0f)
            , BLADE_RADIUS
            , Tier: 3
            , CuttingPower: 1f
            , DeltaTime: 0.01f
        );

        field.Add(PlantKind.Melon, Vector2.zero, fruitCount: 0);
        field.Cut(stroke, obstacles: null, _harvested, _contacts);

        Assert.That(_contacts.Slow, Has.Count.EqualTo(1));
        Assert.That(_contacts.Slow[0].Kind, Is.EqualTo(PlantKind.Melon));
        Assert.That(_contacts.SpeedLimit, Is.EqualTo(2.778f).Within(0.001f));
    }

    [Test]
    public void SlowPassOverATougherPlant_ReportsNoSlowHint()
    {
        var field = CreateField();
        var stroke = new CutStroke(
              new Vector3(-0.01f, 0f, 0f)
            , new Vector3(0f, 0f, 0f)
            , BLADE_RADIUS
            , Tier: 3
            , CuttingPower: 1f
            , DeltaTime: 0.01f
        );

        field.Add(PlantKind.Melon, Vector2.zero, fruitCount: 0);
        field.Cut(stroke, obstacles: null, _harvested, _contacts);

        Assert.That(_contacts.Slow, Is.Empty);
    }

    private static CutStroke Stroke(Vector2 point, int tier, float deltaTime = 1f)
    {
        var world = new Vector3(point.x, 0f, point.y);

        return new CutStroke(world, world, BLADE_RADIUS, tier, CuttingPower: 1f, deltaTime);
    }

    private static PlantObjectField CreateField()
    {
        return new PlantObjectField(new[] {
            Definition(PlantKind.BushLow, tier: 2, toughness: 0.45f, xp: 3, radius: 0.4f),
            Definition(PlantKind.Vegetable, tier: 2, toughness: 0.55f, xp: 4, radius: 0.4f),
            Definition(PlantKind.BushBig, tier: 3, toughness: 0.65f, xp: 4, radius: 0.6f),
            Definition(PlantKind.Melon, tier: 3, toughness: 0.9f, xp: 8, radius: 0.6f),
            Definition(PlantKind.FruitTree, tier: 4, toughness: 2f, xp: 0, radius: 0.6f, fruitCount: 5),
            Definition(PlantKind.GiantFruit, tier: 4, toughness: 2.5f, xp: 0, radius: 1.25f),
        });
    }

    private static PlantDefinition Definition(
          PlantKind kind
        , int tier
        , float toughness
        , int xp
        , float radius
        , int fruitCount = 0
    )
    {
        return new PlantDefinition {
            Kind = kind,
            Representation = PlantRepresentation.Object,
            RequiredTier = tier,
            Toughness = toughness,
            Xp = xp,
            CutZoneRadius = radius,
            FruitCount = fruitCount,
        };
    }
}
