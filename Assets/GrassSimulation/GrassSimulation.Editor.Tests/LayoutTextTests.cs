using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Editor.Tests;

public sealed class LayoutTextTests
{
    private const string BASIC = "......\n.gggg.\n.gggg.\n.S....\n";

    [Test]
    public void Parse_ReadsRowsFromNorthToSouth()
    {
        var layout = Parse(BASIC, 6, 4);

        Assert.That(layout.At(1, 0), Is.EqualTo(LayoutText.SPAWN));
        Assert.That(layout.At(1, 1), Is.EqualTo(LayoutText.GRASS));
        Assert.That(layout.At(0, 3), Is.EqualTo(LayoutText.LAWN));
        Assert.That(layout.Spawn, Is.EqualTo(new Vector2Int(1, 0)));
    }

    [Test]
    public void Parse_ReadsTheSeedAndFruitHeaders()
    {
        var layout = Parse("; seed=7\n; fruit=T:5\n; a plain comment\n" + BASIC, 6, 4);

        Assert.That(layout.Seed, Is.EqualTo(7));
        Assert.That(layout.FruitCounts['T'], Is.EqualTo(5));
    }

    [Test]
    public void Parse_FindsEachConnectedBedAsOneRectangle()
    {
        var layout = Parse("PP..P.\nPP..P.\n......\nS.....\n", 6, 4);

        Assert.That(layout.Beds, Is.EquivalentTo(new[] { new RectInt(0, 2, 2, 2), new RectInt(4, 2, 1, 2) }));
    }

    [Test]
    public void Parse_RejectsABedThatIsNotARectangle()
    {
        Assert.That(Fails("PP....\nP.....\n......\nS.....\n", 6, 4), Is.True);
    }

    [Test]
    public void Parse_RejectsTheWrongRowCount()
    {
        Assert.That(Fails(BASIC, 6, 5), Is.True);
    }

    [Test]
    public void Parse_RejectsTheWrongRowWidth()
    {
        Assert.That(Fails("......\n.gggg.\n.gggg.\n.S...\n", 6, 4), Is.True);
    }

    [Test]
    public void Parse_RejectsUnknownCharacters()
    {
        Assert.That(Fails("......\n.gggg.\n.ggxg.\n.S....\n", 6, 4), Is.True);
    }

    [TestCase("......\n.gggg.\n.gggg.\n......\n")]
    [TestCase("......\n.gggg.\n.gggg.\n.S..S.\n")]
    public void Parse_RejectsAnythingButOneSpawn(string text)
    {
        Assert.That(Fails(text, 6, 4), Is.True);
    }

    private static bool Fails(string text, int width, int height)
        => LayoutText.Parse("T", text, width, height).IsError;

    private static LayoutText Parse(string text, int width, int height)
    {
        var result = LayoutText.Parse("T", text, width, height);

        Assert.That(result.TryGetValue(out var layout), Is.True);
        return layout;
    }
}
