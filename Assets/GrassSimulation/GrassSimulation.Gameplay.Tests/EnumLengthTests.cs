using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class EnumLengthTests
{
    [Test]
    public void PlantKindLength_CoversEveryKindIndex()
    {
        Assert.That(PlantKindExtensions.Length, Is.EqualTo((int)PlantKind.ProtectedFlower + 1));
    }

    [Test]
    public void PropKindLength_CoversEveryKindIndex()
    {
        Assert.That(PropKindExtensions.Length, Is.EqualTo((int)PropKind.Fruit + 1));
    }
}
