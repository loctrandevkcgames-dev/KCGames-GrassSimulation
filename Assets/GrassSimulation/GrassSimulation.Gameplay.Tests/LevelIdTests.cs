using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelIdTests
{
    [TestCase("level-01", true)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void IsValid_FollowsNullOrEmpty(string value, bool expected)
    {
        Assert.That(new LevelId(value).IsValid, Is.EqualTo(expected));
    }

    [Test]
    public void Equality_ComparesWrappedString()
    {
        LevelId left = "level-01";

        Assert.That(left, Is.EqualTo(new LevelId("level-01")));
        Assert.That(left, Is.Not.EqualTo(new LevelId("level-02")));
    }

    [Test]
    public void ImplicitConversion_RoundTripsString()
    {
        string text = new LevelId("level-03");

        Assert.That(text, Is.EqualTo("level-03"));
    }
}
