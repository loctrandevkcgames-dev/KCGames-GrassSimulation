using EncosyTower.ConfigKeys;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class PlayerOptionsTests
{
    private static readonly ConfigKey<bool> s_testKey = new("tests.options.round-trip");

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(s_testKey.Value);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(s_testKey.Value);
    }

    [Test]
    public void Get_ReturnsTheDefaultWhenUnset()
    {
        Assert.That(PlayerOptions.Get(s_testKey, defaultValue: true), Is.True);
        Assert.That(PlayerOptions.Get(s_testKey, defaultValue: false), Is.False);
    }

    [Test]
    public void SetThenGet_RoundTripsBothValues()
    {
        PlayerOptions.Set(s_testKey, value: false);
        Assert.That(PlayerOptions.Get(s_testKey, defaultValue: true), Is.False);

        PlayerOptions.Set(s_testKey, value: true);
        Assert.That(PlayerOptions.Get(s_testKey, defaultValue: false), Is.True);
    }
}
