using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.UI.Tests;

public sealed class PauseOptionsTests
{
    private readonly PauseOption[] _options = { PauseOption.Sound, PauseOption.Haptics, PauseOption.ReduceEffects };

    private readonly bool[] _hadKey = new bool[3];
    private readonly int[] _stored = new int[3];

    [SetUp]
    public void SetUp()
    {
        for (var i = 0; i < _options.Length; i++)
        {
            var key = PauseOptions.GetKey(_options[i]).Value;

            _hadKey[i] = PlayerPrefs.HasKey(key);
            _stored[i] = PlayerPrefs.GetInt(key);
            PlayerPrefs.DeleteKey(key);
        }
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = 0; i < _options.Length; i++)
        {
            var key = PauseOptions.GetKey(_options[i]).Value;

            PlayerPrefs.DeleteKey(key);

            if (_hadKey[i])
            {
                PlayerPrefs.SetInt(key, _stored[i]);
            }
        }
    }

    [Test]
    public void GetKey_MapsEachOptionToItsPlayerOptionsKey()
    {
        var reduceEffects = PauseOptions.GetKey(PauseOption.ReduceEffects);

        Assert.That(PauseOptions.GetKey(PauseOption.Sound).Value, Is.EqualTo(PlayerOptions.Sound.Value));
        Assert.That(PauseOptions.GetKey(PauseOption.Haptics).Value, Is.EqualTo(PlayerOptions.Haptics.Value));
        Assert.That(reduceEffects.Value, Is.EqualTo(PlayerOptions.ReduceEffects.Value));
    }

    [Test]
    public void Read_ReturnsTheDefaultsWhenUnset()
    {
        Assert.That(PauseOptions.Read(PauseOption.Sound), Is.EqualTo(PlayerOptions.DEFAULT_SOUND));
        Assert.That(PauseOptions.Read(PauseOption.Haptics), Is.EqualTo(PlayerOptions.DEFAULT_HAPTICS));
        Assert.That(PauseOptions.Read(PauseOption.ReduceEffects), Is.EqualTo(PlayerOptions.DEFAULT_REDUCE_EFFECTS));
    }

    [Test]
    public void Write_PersistsEachOptionIndependently()
    {
        PauseOptions.Write(option: PauseOption.Sound, value: false);
        PauseOptions.Write(option: PauseOption.ReduceEffects, value: true);

        Assert.That(PlayerOptions.GetSound(), Is.False);
        Assert.That(PlayerOptions.GetHaptics(), Is.EqualTo(PlayerOptions.DEFAULT_HAPTICS));
        Assert.That(PlayerOptions.GetReduceEffects(), Is.True);
        Assert.That(PauseOptions.Read(PauseOption.Sound), Is.False);
        Assert.That(PauseOptions.Read(PauseOption.ReduceEffects), Is.True);
    }

    [Test]
    public void GetLabel_NamesEachOption()
    {
        Assert.That(PauseOptions.GetLabel(PauseOption.Sound), Is.EqualTo("Âm thanh"));
        Assert.That(PauseOptions.GetLabel(PauseOption.Haptics), Is.EqualTo("Rung"));
        Assert.That(PauseOptions.GetLabel(PauseOption.ReduceEffects), Is.EqualTo("Giảm hiệu ứng"));
    }
}
