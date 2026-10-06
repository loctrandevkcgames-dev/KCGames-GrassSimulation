using NUnit.Framework;

namespace GrassSimulation.Audio.Tests;

public sealed class VolumeFadeTests
{
    [Test]
    public void Step_MovesLinearlyAndFinishes()
    {
        var fade = new VolumeFade();

        fade.Start(from: 0f, to: 1f, seconds: 1f);
        fade.Step(0.25f);

        Assert.That(fade.Value, Is.EqualTo(0.25f).Within(1e-5f));
        Assert.That(fade.IsDone, Is.False);

        fade.Step(2f);

        Assert.That(fade.Value, Is.EqualTo(1f));
        Assert.That(fade.IsDone, Is.True);
    }

    [Test]
    public void Start_WithZeroSecondsJumpsToTheTarget()
    {
        var fade = new VolumeFade();

        fade.Start(from: 1f, to: 0f, seconds: 0f);

        Assert.That(fade.Value, Is.EqualTo(0f));
        Assert.That(fade.IsDone, Is.True);
    }
}
