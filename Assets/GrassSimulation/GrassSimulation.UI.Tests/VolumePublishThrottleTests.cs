using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class VolumePublishThrottleTests
{
    [Test]
    public void Tick_FiresOnTheFirstTickAfterAChange()
    {
        var throttle = new VolumePublishThrottle();

        throttle.MarkChanged();

        Assert.That(throttle.Tick(deltaTime: 0.016f), Is.True);
        Assert.That(throttle.Tick(deltaTime: 0.016f), Is.False);
    }

    [Test]
    public void Tick_WaitsTheIntervalBetweenChanges()
    {
        var throttle = new VolumePublishThrottle();

        throttle.MarkChanged();
        Assert.That(throttle.Tick(deltaTime: 0.016f), Is.True);

        throttle.MarkChanged();
        Assert.That(throttle.Tick(deltaTime: 0.05f), Is.False);
        Assert.That(throttle.Tick(deltaTime: 0.05f), Is.True);
    }

    [Test]
    public void Tick_StaysSilentWithoutAChange()
    {
        var throttle = new VolumePublishThrottle();

        Assert.That(throttle.Tick(deltaTime: 1f), Is.False);
        Assert.That(throttle.Tick(deltaTime: 1f), Is.False);
    }

    [Test]
    public void Flush_ReturnsThePendingChangeOnce()
    {
        var throttle = new VolumePublishThrottle();

        throttle.MarkChanged();

        Assert.That(throttle.Flush(), Is.True);
        Assert.That(throttle.Flush(), Is.False);
        Assert.That(throttle.Tick(deltaTime: 1f), Is.False);
    }

    [Test]
    public void Flush_ReturnsFalseWithoutAChange()
    {
        var throttle = new VolumePublishThrottle();

        Assert.That(throttle.Flush(), Is.False);
    }
}
