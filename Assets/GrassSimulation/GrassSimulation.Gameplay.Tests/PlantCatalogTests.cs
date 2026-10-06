using System;
using NUnit.Framework;
using UnityEditor;

namespace GrassSimulation.Gameplay.Tests;

public sealed class PlantCatalogTests
{
    private const string CATALOG_PATH = "Assets/GrassSimulation/Addressables/assets-shared/database/PlantCatalog.asset";

    private PlantCatalog _catalog;

    [SetUp]
    public void SetUp()
    {
        _catalog = AssetDatabase.LoadAssetAtPath<PlantCatalog>(CATALOG_PATH);

        Assert.That(_catalog, Is.Not.Null, CATALOG_PATH);
    }

    [Test]
    public void EveryPlantKind_HasAnEntry()
    {
        foreach (PlantKind kind in Enum.GetValues(typeof(PlantKind)))
        {
            if (kind == PlantKind.None)
            {
                continue;
            }

            Assert.That(_catalog.TryGet(kind, out _), Is.True, kind.ToString());
        }
    }

    [TestCase(PlantKind.Grass, "grass_common", 1, 0.12f, 1, 0.25f, PlantRepresentation.FieldCell)]
    [TestCase(PlantKind.HarvestFlower, "flower_small", 1, 0.18f, 1, 0.25f, PlantRepresentation.FieldCell)]
    [TestCase(PlantKind.ThickGrass, "grass_thick", 2, 0.3f, 2, 0.25f, PlantRepresentation.FieldCell)]
    [TestCase(PlantKind.BushLow, "bush_low", 2, 0.45f, 3, 0.4f, PlantRepresentation.Object)]
    [TestCase(PlantKind.Vegetable, "veg_medium", 2, 0.55f, 4, 0.4f, PlantRepresentation.Object)]
    [TestCase(PlantKind.BushBig, "bush_big", 3, 0.65f, 4, 0.6f, PlantRepresentation.Object)]
    [TestCase(PlantKind.Melon, "melon", 3, 0.9f, 8, 0.6f, PlantRepresentation.Object)]
    [TestCase(PlantKind.FruitTree, "fruit_tree", 4, 2f, 0, 0.6f, PlantRepresentation.Object)]
    [TestCase(PlantKind.GiantFruit, "fruit_giant", 4, 2.5f, 0, 1.25f, PlantRepresentation.Object)]
    public void Plant_MatchesTheBalancePlantTable(
          PlantKind kind
        , string key
        , int tier
        , float toughness
        , int xp
        , float zoneRadius
        , PlantRepresentation representation
    )
    {
        Assert.That(_catalog.TryGet(kind, out var plant), Is.True);
        Assert.That(plant.Key, Is.EqualTo(key));
        Assert.That(plant.RequiredTier, Is.EqualTo(tier));
        Assert.That(plant.Toughness, Is.EqualTo(toughness).Within(1e-4f));
        Assert.That(plant.Xp, Is.EqualTo(xp));
        Assert.That(plant.CutZoneRadius, Is.EqualTo(zoneRadius).Within(1e-4f));
        Assert.That(plant.Representation, Is.EqualTo(representation));
    }

    [Test]
    public void Tier4Plants_GiveNoXp()
    {
        var plants = _catalog.Plants;

        for (var i = 0; i < plants.Length; i++)
        {
            if (plants[i].RequiredTier == 4)
            {
                Assert.That(plants[i].Xp, Is.Zero, plants[i].Kind.ToString());
            }
        }
    }

    [Test]
    public void ProtectedFlower_IsNeverCutAndGivesNoXp()
    {
        Assert.That(_catalog.TryGet(PlantKind.ProtectedFlower, out var plant), Is.True);
        Assert.That(plant.IsProtected, Is.True);
        Assert.That(plant.Xp, Is.Zero);
        Assert.That(plant.Representation, Is.EqualTo(PlantRepresentation.FieldCell));
    }

    [Test]
    public void FruitTree_AnnouncesFiveFruitByDefault()
    {
        Assert.That(_catalog.TryGet(PlantKind.FruitTree, out var plant), Is.True);
        Assert.That(plant.FruitCount, Is.EqualTo(5));
    }

    [Test]
    public void ObjectPlants_HaveAtLeastOnePrefabWithAView()
    {
        var plants = _catalog.Plants;

        for (var i = 0; i < plants.Length; i++)
        {
            if (plants[i].Representation != PlantRepresentation.Object)
            {
                continue;
            }

            var prefabs = plants[i].ObjectPrefabs;

            Assert.That(prefabs, Is.Not.Null.And.Not.Empty, plants[i].Kind.ToString());

            for (var p = 0; p < prefabs.Length; p++)
            {
                Assert.That(prefabs[p].GetComponent<PlantObjectView>(), Is.Not.Null, prefabs[p].name);
            }
        }
    }

    [Test]
    public void FieldCellPlants_HaveARenderMaterial()
    {
        var plants = _catalog.Plants;

        for (var i = 0; i < plants.Length; i++)
        {
            if (plants[i].Representation == PlantRepresentation.FieldCell)
            {
                Assert.That(plants[i].Material, Is.Not.Null, plants[i].Kind.ToString());
            }
        }
    }
}
