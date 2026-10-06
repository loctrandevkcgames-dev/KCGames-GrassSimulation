using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelRulesTests
{
    private TestAssets _assets;

    [SetUp]
    public void SetUp()
    {
        _assets = new TestAssets();
    }

    [TearDown]
    public void TearDown()
    {
        _assets.Dispose();
    }

    [TestCase(LevelType.Tutorial, false, false)]
    [TestCase(LevelType.Relax, false, false)]
    [TestCase(LevelType.Normal, true, true)]
    [TestCase(LevelType.Hard, true, true)]
    public void Resolve_SetsTimedAndLoseByLevelType(LevelType type, bool isTimed, bool canLose)
    {
        var level = _assets.CreateLevel(type, timeLimit: 60f);
        var rules = LevelRules.Resolve(level, GameRulesValues.Default);

        Assert.That(rules.IsTimed, Is.EqualTo(isTimed));
        Assert.That(rules.CanLose, Is.EqualTo(canLose));
        Assert.That(rules.TimeLimit, Is.EqualTo(isTimed ? 60f : 0f));
    }

    [Test]
    public void Resolve_MultipliesTheTimeLimit()
    {
        var level = _assets.CreateLevel(timeLimit: 60f);
        var rules = LevelRules.Resolve(level, GameRulesValues.Default with { TimerMultiplier = 1.2f });

        Assert.That(rules.TimeLimit, Is.EqualTo(72f).Within(1e-4f));
    }

    [Test]
    public void FailsOnProtectedHits_NeedsFailModeAndALevelThatCanLose()
    {
        var failValues = GameRulesValues.Default with { ProtectedMode = ProtectedMode.Fail };
        var normal = _assets.CreateLevel(60f);
        var tutorial = _assets.CreateLevel(LevelType.Tutorial, 60f);

        Assert.That(LevelRules.Resolve(normal, GameRulesValues.Default).FailsOnProtectedHits, Is.False);
        Assert.That(LevelRules.Resolve(normal, failValues).FailsOnProtectedHits, Is.True);
        Assert.That(LevelRules.Resolve(tutorial, failValues).FailsOnProtectedHits, Is.False);
    }

    [Test]
    public void Beds_AreTheProtectedZonesOfTheLevel()
    {
        var level = _assets.CreateLevel(timeLimit: 60f);

        _assets.SetBeds(level, new RectInt(1, 1, 2, 2), new RectInt(4, 4, 3, 1));

        Assert.That(level.Beds.Length, Is.EqualTo(2));
        Assert.That(level.Beds[0], Is.EqualTo(new RectInt(1, 1, 2, 2)));
        Assert.That(level.Beds[1], Is.EqualTo(new RectInt(4, 4, 3, 1)));
    }

    [Test]
    public void Beds_AreEmptyWithoutProtectedZones()
    {
        var level = _assets.CreateLevel(timeLimit: 60f);

        Assert.That(level.Beds.Length, Is.Zero);
    }
}
