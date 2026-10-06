using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.UI.Tests;

public sealed class VolumeOptionsTests
{
    private readonly VolumeOption[] _options = { VolumeOption.Music, VolumeOption.Sfx };

    private readonly bool[] _hadKey = new bool[2];
    private readonly float[] _stored = new float[2];

    [SetUp]
    public void SetUp()
    {
        for (var i = 0; i < _options.Length; i++)
        {
            var key = VolumeOptions.GetKey(_options[i]).Value;

            _hadKey[i] = PlayerPrefs.HasKey(key);
            _stored[i] = PlayerPrefs.GetFloat(key);
            PlayerPrefs.DeleteKey(key);
        }
    }

    [TearDown]
    public void TearDown()
    {
        for (var i = 0; i < _options.Length; i++)
        {
            var key = VolumeOptions.GetKey(_options[i]).Value;

            PlayerPrefs.DeleteKey(key);

            if (_hadKey[i])
            {
                PlayerPrefs.SetFloat(key, _stored[i]);
            }
        }
    }

    [Test]
    public void GetKey_MapsEachOptionToItsPlayerOptionsKey()
    {
        Assert.That(VolumeOptions.GetKey(VolumeOption.Music).Value, Is.EqualTo(PlayerOptions.MusicVolume.Value));
        Assert.That(VolumeOptions.GetKey(VolumeOption.Sfx).Value, Is.EqualTo(PlayerOptions.SfxVolume.Value));
    }

    [Test]
    public void Read_ReturnsTheDefaultsWhenUnset()
    {
        Assert.That(VolumeOptions.Read(VolumeOption.Music), Is.EqualTo(PlayerOptions.DEFAULT_MUSIC_VOLUME));
        Assert.That(VolumeOptions.Read(VolumeOption.Sfx), Is.EqualTo(PlayerOptions.DEFAULT_SFX_VOLUME));
    }

    [Test]
    public void Write_PersistsEachOptionIndependently()
    {
        VolumeOptions.Write(option: VolumeOption.Music, value: 0.25f);

        Assert.That(PlayerOptions.GetMusicVolume(), Is.EqualTo(0.25f));
        Assert.That(PlayerOptions.GetSfxVolume(), Is.EqualTo(PlayerOptions.DEFAULT_SFX_VOLUME));
        Assert.That(VolumeOptions.Read(VolumeOption.Music), Is.EqualTo(0.25f));

        VolumeOptions.Write(option: VolumeOption.Sfx, value: 0.5f);

        Assert.That(VolumeOptions.Read(VolumeOption.Music), Is.EqualTo(0.25f));
        Assert.That(VolumeOptions.Read(VolumeOption.Sfx), Is.EqualTo(0.5f));
    }

    [Test]
    public void Write_ClampsToTheUnitRange()
    {
        VolumeOptions.Write(option: VolumeOption.Music, value: 1.5f);
        VolumeOptions.Write(option: VolumeOption.Sfx, value: -0.2f);

        Assert.That(VolumeOptions.Read(VolumeOption.Music), Is.EqualTo(1f));
        Assert.That(VolumeOptions.Read(VolumeOption.Sfx), Is.EqualTo(0f));
    }

    [Test]
    public void GetLabel_NamesEachOption()
    {
        Assert.That(VolumeOptions.GetLabel(VolumeOption.Music), Is.EqualTo("Nhạc nền"));
        Assert.That(VolumeOptions.GetLabel(VolumeOption.Sfx), Is.EqualTo("Âm hiệu ứng"));
    }
}
