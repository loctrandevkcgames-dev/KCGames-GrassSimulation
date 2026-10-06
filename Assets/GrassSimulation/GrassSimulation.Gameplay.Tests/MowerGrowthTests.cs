using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class MowerGrowthTests
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

    [Test]
    public void WideBlade_AddsToTheBaseRadiusWithoutACap()
    {
        var growth = new MowerGrowth(_assets.CreateMachine());

        growth.AddXp(480);

        for (var i = 0; i < 3; i++)
        {
            Assert.That(growth.TryChooseUpgrade(0), Is.True);
        }

        Assert.That(growth.Stats.CutRadius, Is.EqualTo(1.1f).Within(1e-5f));
        Assert.That(growth.GetStatsWithUpgrade(0).CutRadius, Is.EqualTo(1.25f).Within(1e-5f));
    }

    [Test]
    public void StrongEngine_AddsToTheBaseSpeedWithoutACap()
    {
        var growth = new MowerGrowth(_assets.CreateMachine());

        growth.AddXp(480);

        for (var i = 0; i < 3; i++)
        {
            Assert.That(growth.TryChooseUpgrade(1), Is.True);
        }

        Assert.That(growth.Stats.Speed, Is.EqualTo(4.75f).Within(1e-5f));
        Assert.That(growth.GetStatsWithUpgrade(1).Speed, Is.EqualTo(5f).Within(1e-5f));
    }

    [Test]
    public void GetIntroducedTier_IsTheHighestMaxTierUpToTheLevel()
    {
        var first = _assets.CreateLevel(timeLimit: 10f);
        var second = _assets.CreateLevel(timeLimit: 10f);
        var third = _assets.CreateLevel(timeLimit: 10f);
        var fourth = _assets.CreateLevel(timeLimit: 10f);

        _assets.SetMaxTier(first, 1);
        _assets.SetMaxTier(second, 2);
        _assets.SetMaxTier(third, 1);
        _assets.SetMaxTier(fourth, 3);

        var catalog = _assets.CreateCatalog(first, second, third, fourth);

        Assert.That(catalog.GetIntroducedTier(0), Is.EqualTo(1));
        Assert.That(catalog.GetIntroducedTier(1), Is.EqualTo(2));
        Assert.That(catalog.GetIntroducedTier(2), Is.EqualTo(2));
        Assert.That(catalog.GetIntroducedTier(3), Is.EqualTo(3));
        Assert.That(catalog.GetIntroducedTier(99), Is.EqualTo(3));
    }
}
