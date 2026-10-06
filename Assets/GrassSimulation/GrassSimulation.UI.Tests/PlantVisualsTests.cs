using System;
using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class PlantVisualsTests
{
    [Test]
    public void EveryPlantKind_HasLabelIconAndOpaqueColors()
    {
        foreach (PlantKind kind in Enum.GetValues(typeof(PlantKind)))
        {
            var colors = PlantVisuals.GetColors(kind);

            Assert.That(PlantVisuals.GetLabel(kind), Is.Not.Empty, kind.ToString());
            Assert.That(Enum.IsDefined(typeof(PlantIcon), PlantVisuals.GetIcon(kind)), Is.True, kind.ToString());
            Assert.That(colors.Box.a, Is.EqualTo(1f), kind.ToString());
            Assert.That(colors.Icon.a, Is.EqualTo(1f), kind.ToString());
            Assert.That(colors.BarFill.a, Is.EqualTo(1f), kind.ToString());
            Assert.That(colors.BarTrack.a, Is.EqualTo(1f), kind.ToString());
        }
    }

    [TestCase(PlantKind.HarvestFlower, PlantIcon.Flower)]
    [TestCase(PlantKind.ProtectedFlower, PlantIcon.Flower)]
    [TestCase(PlantKind.LowBush, PlantIcon.Bush)]
    [TestCase(PlantKind.HardBush, PlantIcon.Bush)]
    [TestCase(PlantKind.ThickGrass, PlantIcon.Grass)]
    public void GetIcon_GroupsKindsByShape(PlantKind kind, PlantIcon expected)
    {
        Assert.That(PlantVisuals.GetIcon(kind), Is.EqualTo(expected));
    }

    [Test]
    public void GetLabel_IsUniquePerKind()
    {
        var labels = new System.Collections.Generic.HashSet<string>();

        foreach (PlantKind kind in Enum.GetValues(typeof(PlantKind)))
        {
            if (kind == PlantKind.None)
            {
                continue;
            }

            Assert.That(labels.Add(PlantVisuals.GetLabel(kind)), Is.True, kind.ToString());
        }
    }
}
