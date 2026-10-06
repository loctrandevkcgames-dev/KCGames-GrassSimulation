using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class CameraBlendTests
{
    private const float EPSILON = 1e-5f;

    [Test]
    public void New_StartsAtZero()
    {
        var blend = new CameraBlend();

        Assert.That(blend.Weight, Is.EqualTo(0f));
        Assert.That(blend.Eased, Is.EqualTo(0f));
    }

    [Test]
    public void Step_MovesTowardTargetOverSeconds()
    {
        var blend = new CameraBlend();

        blend.Step(target: 1f, seconds: 0.5f, deltaTime: 0.25f);

        Assert.That(blend.Weight, Is.EqualTo(0.5f).Within(EPSILON));
        Assert.That(blend.Eased, Is.EqualTo(0.5f).Within(EPSILON));
    }

    [Test]
    public void Step_ClampsAtTarget()
    {
        var blend = new CameraBlend();

        blend.Step(target: 1f, seconds: 0.5f, deltaTime: 5f);

        Assert.That(blend.Weight, Is.EqualTo(1f));
        Assert.That(blend.Eased, Is.EqualTo(1f).Within(EPSILON));
    }

    [Test]
    public void Step_ZeroSecondsSnaps()
    {
        var blend = new CameraBlend();

        blend.Step(target: 1f, seconds: 0f, deltaTime: 0.01f);

        Assert.That(blend.Weight, Is.EqualTo(1f));
    }

    [Test]
    public void Step_RetargetContinuesFromCurrent()
    {
        var blend = new CameraBlend();

        blend.Step(target: 1f, seconds: 1f, deltaTime: 0.6f);
        blend.Step(target: 0f, seconds: 1f, deltaTime: 0.2f);

        Assert.That(blend.Weight, Is.EqualTo(0.4f).Within(EPSILON));
    }

    [Test]
    public void Snap_SetsClampedWeight()
    {
        var blend = new CameraBlend();

        blend.Snap(3f);

        Assert.That(blend.Weight, Is.EqualTo(1f));
    }
}
