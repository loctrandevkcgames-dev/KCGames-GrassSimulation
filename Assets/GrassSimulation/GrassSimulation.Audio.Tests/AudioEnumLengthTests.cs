using NUnit.Framework;

namespace GrassSimulation.Audio.Tests;

public sealed class AudioEnumLengthTests
{
    [Test]
    public void SoundIdLength_CoversEveryId()
    {
        Assert.That(SoundIdExtensions.Length, Is.EqualTo((int)SoundId.JingleLose + 1));
    }

    [Test]
    public void AudioBusLength_CoversEveryBus()
    {
        Assert.That(AudioBusExtensions.Length, Is.EqualTo((int)AudioBus.Ui + 1));
    }
}
