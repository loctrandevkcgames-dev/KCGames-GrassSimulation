using EncosyTower.ConfigKeys;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class PlayerOptionsTests
{
    private static readonly ConfigKey<bool> s_testKey = new("tests.options.round-trip");
    private static readonly ConfigKey<float> s_volumeKey = new("tests.options.volume");

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey(s_testKey.Value);
        PlayerPrefs.DeleteKey(s_volumeKey.Value);
    }

    [TearDown]
    public void TearDown()
    {
        PlayerPrefs.DeleteKey(s_testKey.Value);
        PlayerPrefs.DeleteKey(s_volumeKey.Value);
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

    [Test]
    public void GetVolume_ReturnsTheDefaultWhenUnset()
    {
        Assert.That(PlayerOptions.Get(s_volumeKey, defaultValue: 0.6f), Is.EqualTo(0.6f));
    }

    [Test]
    public void SetVolume_RoundTripsAndClampsToTheUnitRange()
    {
        PlayerOptions.SetVolume(s_volumeKey, 0.25f);
        Assert.That(PlayerOptions.Get(s_volumeKey, defaultValue: 1f), Is.EqualTo(0.25f));

        PlayerOptions.SetVolume(s_volumeKey, 3f);
        Assert.That(PlayerOptions.Get(s_volumeKey, defaultValue: 0f), Is.EqualTo(1f));

        PlayerOptions.SetVolume(s_volumeKey, -1f);
        Assert.That(PlayerOptions.Get(s_volumeKey, defaultValue: 1f), Is.EqualTo(0f));
    }
}
