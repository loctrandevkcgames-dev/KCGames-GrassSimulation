using NUnit.Framework;

namespace GrassSimulation.Audio.Tests;

public sealed class AudioVolumeTests
{
    [Test]
    public void ToDecibels_MapsSilenceAndFullScale()
    {
        Assert.That(AudioVolume.ToDecibels(0f), Is.EqualTo(AudioVolume.MIN_DB));
        Assert.That(AudioVolume.ToDecibels(1e-5f), Is.EqualTo(AudioVolume.MIN_DB));
        Assert.That(AudioVolume.ToDecibels(1f), Is.EqualTo(0f));
        Assert.That(AudioVolume.ToDecibels(2f), Is.EqualTo(0f));
    }

    [Test]
    public void ToDecibels_HalfScaleIsAboutMinusSix()
    {
        Assert.That(AudioVolume.ToDecibels(0.5f), Is.EqualTo(-6.02f).Within(0.01f));
    }

    [Test]
    public void FromDecibels_InvertsToDecibels()
    {
        Assert.That(AudioVolume.FromDecibels(AudioVolume.ToDecibels(0.25f)), Is.EqualTo(0.25f).Within(1e-4f));
        Assert.That(AudioVolume.FromDecibels(AudioVolume.MIN_DB), Is.EqualTo(0f));
        Assert.That(AudioVolume.FromDecibels(0f), Is.EqualTo(1f));
    }

    [Test]
    public void BusDecibels_AddsTheSliderToTheBase()
    {
        Assert.That(AudioVolume.BusDecibels(-14f, slider: 1f, muted: false), Is.EqualTo(-14f));
        Assert.That(AudioVolume.BusDecibels(-14f, slider: 0.5f, muted: false), Is.EqualTo(-20.02f).Within(0.01f));
    }

    [Test]
    public void BusDecibels_ClampsToTheFloorWhenSilentOrMuted()
    {
        Assert.That(AudioVolume.BusDecibels(-14f, slider: 0f, muted: false), Is.EqualTo(AudioVolume.MIN_DB));
        Assert.That(AudioVolume.BusDecibels(0f, slider: 1f, muted: true), Is.EqualTo(AudioVolume.MIN_DB));
    }
}
