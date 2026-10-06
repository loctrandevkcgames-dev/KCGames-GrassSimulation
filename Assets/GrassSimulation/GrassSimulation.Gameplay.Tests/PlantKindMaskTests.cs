using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class PlantKindMaskTests
{
    [Test]
    public void FromTier_CollectsKindsRequiringExactlyThatTier()
    {
        var plants = new[] {
            new PlantSettings { Kind = PlantKind.Grass, RequiredTier = 1 },
            new PlantSettings { Kind = PlantKind.LowBush, RequiredTier = 2 },
            new PlantSettings { Kind = PlantKind.HardBush, RequiredTier = 3 },
            new PlantSettings { Kind = PlantKind.ThickGrass, RequiredTier = 3 },
        };

        var mask = PlantKindMask.FromTier(plants, tier: 3);

        Assert.That(mask.Count, Is.EqualTo(2));
        Assert.That(mask.Contains(PlantKind.HardBush), Is.True);
        Assert.That(mask.Contains(PlantKind.ThickGrass), Is.True);
        Assert.That(mask.Contains(PlantKind.LowBush), Is.False);
    }

    [Test]
    public void FromTier_IsEmptyWhenNothingUnlocksAtThatTier()
    {
        var plants = new[] { new PlantSettings { Kind = PlantKind.Grass, RequiredTier = 1 } };

        Assert.That(PlantKindMask.FromTier(plants, tier: 2), Is.EqualTo(default(PlantKindMask)));
        Assert.That(PlantKindMask.FromTier(plants: default, tier: 1).Count, Is.Zero);
    }

    [Test]
    public void FromTier_IgnoresPlantKindNone()
    {
        var plants = new[] { new PlantSettings { Kind = PlantKind.None, RequiredTier = 0 } };

        Assert.That(PlantKindMask.FromTier(plants, tier: 0).Count, Is.Zero);
    }

    [Test]
    public void PlantKindLength_FitsInTheMaskBits()
    {
        Assert.That(PlantKindExtensions.Length, Is.LessThanOrEqualTo(32));
    }
}
