using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class EnumLengthTests
{
    [Test]
    public void PlantKindLength_CoversEveryKindIndex()
    {
        Assert.That(PlantKindExtensions.Length, Is.EqualTo((int)PlantKind.GiantFruit + 1));
    }
}
