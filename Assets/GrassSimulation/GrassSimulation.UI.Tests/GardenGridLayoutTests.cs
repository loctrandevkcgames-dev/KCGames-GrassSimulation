using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class GardenGridLayoutTests
{
    private const int TILES = 10;

    private static GardenTileState[] Map(int levelCount, int completedCount, int nextLevelIndex)
    {
        var first = GardenGridLayout.GetFirstLevelIndex(
              levelCount: levelCount
            , nextLevelIndex: nextLevelIndex
            , tileCount: TILES
        );

        var result = new GardenTileState[TILES];

        for (var i = 0; i < TILES; i++)
        {
            result[i] = GardenGridLayout.GetTileState(
                  levelIndex: first + i
                , levelCount: levelCount
                , completedCount: completedCount
                , nextLevelIndex: nextLevelIndex
            );
        }

        return result;
    }

    [Test]
    public void GetTileState_FreshSave_FirstIsNextRestLocked()
    {
        var s = Map(levelCount: 10, completedCount: 0, nextLevelIndex: 0);

        Assert.That(s[0], Is.EqualTo(GardenTileState.Next));

        for (var i = 1; i < TILES; i++)
        {
            Assert.That(s[i], Is.EqualTo(GardenTileState.Locked));
        }
    }

    [Test]
    public void GetTileState_PartialProgress_CompletedThenNextThenLocked()
    {
        var s = Map(levelCount: 10, completedCount: 3, nextLevelIndex: 3);

        for (var i = 0; i < 3; i++)
        {
            Assert.That(s[i], Is.EqualTo(GardenTileState.Completed));
        }

        Assert.That(s[3], Is.EqualTo(GardenTileState.Next));

        for (var i = 4; i < TILES; i++)
        {
            Assert.That(s[i], Is.EqualTo(GardenTileState.Locked));
        }
    }

    [Test]
    public void GetTileState_AllDone_AllCompleted()
    {
        foreach (var state in Map(levelCount: 10, completedCount: 10, nextLevelIndex: 9))
        {
            Assert.That(state, Is.EqualTo(GardenTileState.Completed));
        }
    }

    [Test]
    public void GetTileState_FewerLevelsThanTiles_RestEmpty()
    {
        var s = Map(levelCount: 6, completedCount: 2, nextLevelIndex: 2);

        for (var i = 6; i < TILES; i++)
        {
            Assert.That(s[i], Is.EqualTo(GardenTileState.Empty));
        }

        Assert.That(s[0], Is.EqualTo(GardenTileState.Completed));
        Assert.That(s[1], Is.EqualTo(GardenTileState.Completed));
        Assert.That(s[2], Is.EqualTo(GardenTileState.Next));
    }

    [Test]
    public void GetTileState_SecondPage_FollowsNext()
    {
        var s = Map(levelCount: 30, completedCount: 13, nextLevelIndex: 13);

        for (var i = 0; i < 3; i++)
        {
            Assert.That(s[i], Is.EqualTo(GardenTileState.Completed));
        }

        Assert.That(s[3], Is.EqualTo(GardenTileState.Next));
    }

    [Test]
    public void GetTileState_PageBoundary_NextIsFirstTile()
    {
        var s = Map(levelCount: 30, completedCount: 10, nextLevelIndex: 10);

        Assert.That(s[0], Is.EqualTo(GardenTileState.Next));
    }

    [Test]
    public void GetTileState_ManyLevelsAllDone_AllCompleted()
    {
        foreach (var state in Map(levelCount: 30, completedCount: 30, nextLevelIndex: 29))
        {
            Assert.That(state, Is.EqualTo(GardenTileState.Completed));
        }
    }

    [Test]
    public void GetTileState_NoLevels_AllEmpty()
    {
        foreach (var state in Map(levelCount: 0, completedCount: 0, nextLevelIndex: 0))
        {
            Assert.That(state, Is.EqualTo(GardenTileState.Empty));
        }
    }

    [Test]
    public void GetTileState_OutOfOrderCompletion_ShowsLocked()
    {
        var state = GardenGridLayout.GetTileState(
              levelIndex: 3
            , levelCount: 10
            , completedCount: 4
            , nextLevelIndex: 2
        );

        Assert.That(state, Is.EqualTo(GardenTileState.Locked));
    }

    [Test]
    public void GetTileState_NextBeyondLevelCount_ClampsToLastLevel()
    {
        var s = Map(levelCount: 10, completedCount: 3, nextLevelIndex: 99);

        for (var i = 0; i < 9; i++)
        {
            Assert.That(s[i], Is.EqualTo(GardenTileState.Completed));
        }

        Assert.That(s[9], Is.EqualTo(GardenTileState.Next));
    }

    [Test]
    public void GetTileState_NegativeNext_ClampsToFirstLevel()
    {
        var s = Map(levelCount: 10, completedCount: 0, nextLevelIndex: -5);

        Assert.That(s[0], Is.EqualTo(GardenTileState.Next));
        Assert.That(s[1], Is.EqualTo(GardenTileState.Locked));
    }

    [Test]
    public void GetFirstLevelIndex_ZeroTiles_ReturnsZero()
    {
        Assert.That(GardenGridLayout.GetFirstLevelIndex(levelCount: 10, nextLevelIndex: 3, tileCount: 0), Is.EqualTo(0));
    }

    [Test]
    public void GetFirstLevelIndex_NextBeyondLevelCount_ClampsToLastPage()
    {
        Assert.That(GardenGridLayout.GetFirstLevelIndex(levelCount: 10, nextLevelIndex: 99, tileCount: TILES), Is.EqualTo(0));
        Assert.That(GardenGridLayout.GetFirstLevelIndex(levelCount: 30, nextLevelIndex: 99, tileCount: TILES), Is.EqualTo(20));
    }

    [TestCase(13, 10)]
    [TestCase(29, 20)]
    public void GetFirstLevelIndex_ManyLevels_PagesByTileCount(int nextLevelIndex, int expected)
    {
        var first = GardenGridLayout.GetFirstLevelIndex(levelCount: 30, nextLevelIndex: nextLevelIndex, tileCount: TILES);

        Assert.That(first, Is.EqualTo(expected));
    }

    [TestCase(GardenTileState.Empty, 0f, 0f, 0f, 0f)]
    [TestCase(GardenTileState.Locked, 0x4E / 255f, 0x9A / 255f, 0x2E / 255f, 0.45f)]
    [TestCase(GardenTileState.Next, 0xF4 / 255f, 0xB7 / 255f, 0x40 / 255f, 1f)]
    [TestCase(GardenTileState.Completed, 0xA9 / 255f, 0xC9 / 255f, 0x8A / 255f, 1f)]
    public void GetBackground_EachState_MatchesPalette(GardenTileState state, float r, float g, float b, float a)
    {
        var color = GardenTileVisuals.GetBackground(state);

        Assert.That(color.r, Is.EqualTo(r).Within(0.005f));
        Assert.That(color.g, Is.EqualTo(g).Within(0.005f));
        Assert.That(color.b, Is.EqualTo(b).Within(0.005f));
        Assert.That(color.a, Is.EqualTo(a).Within(0.005f));
    }

    [TestCase(GardenTileState.Empty, 0f, 0f, 0f, 0f)]
    [TestCase(GardenTileState.Locked, 1f, 1f, 1f, 0.8f)]
    [TestCase(GardenTileState.Next, 1f, 1f, 1f, 1f)]
    [TestCase(GardenTileState.Completed, 0x2E / 255f, 0x7D / 255f, 0x32 / 255f, 1f)]
    public void GetIconColor_EachState_MatchesPalette(GardenTileState state, float r, float g, float b, float a)
    {
        var color = GardenTileVisuals.GetIconColor(state);

        Assert.That(color.r, Is.EqualTo(r).Within(0.005f));
        Assert.That(color.g, Is.EqualTo(g).Within(0.005f));
        Assert.That(color.b, Is.EqualTo(b).Within(0.005f));
        Assert.That(color.a, Is.EqualTo(a).Within(0.005f));
    }
}
