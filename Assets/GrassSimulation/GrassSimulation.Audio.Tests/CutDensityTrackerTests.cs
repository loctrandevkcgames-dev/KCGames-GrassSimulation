using NUnit.Framework;

namespace GrassSimulation.Audio.Tests;

public sealed class CutDensityTrackerTests
{
    [Test]
    public void Step_StartsAtZero()
    {
        var tracker = new CutDensityTracker(smoothing: 0.1f, silenceTimeout: 0.2f);

        Assert.That(tracker.Step(time: 1f, deltaTime: 0.016f), Is.EqualTo(0f));
    }

    [Test]
    public void Step_ApproachesTheLatestWindow()
    {
        var tracker = new CutDensityTracker(smoothing: 0.1f, silenceTimeout: 0.2f);
        var time = 0f;
        var value = 0f;

        tracker.Push(cells: 50, time);

        for (var i = 0; i < 10; i++)
        {
            time += 0.016f;
            value = tracker.Step(time, deltaTime: 0.016f);
        }

        Assert.That(value, Is.GreaterThan(10f).And.LessThan(50f));
    }

    [Test]
    public void Step_FallsToZeroAfterTheSilenceTimeout()
    {
        var tracker = new CutDensityTracker(smoothing: 0.1f, silenceTimeout: 0.2f);
        var time = 0f;

        tracker.Push(cells: 50, time);

        for (var i = 0; i < 400; i++)
        {
            time += 0.016f;
            tracker.Step(time, deltaTime: 0.016f);
        }

        Assert.That(tracker.Value, Is.EqualTo(0f));
    }

    [Test]
    public void Clear_DropsTheDensity()
    {
        var tracker = new CutDensityTracker(smoothing: 0.1f, silenceTimeout: 0.2f);

        tracker.Push(cells: 50, time: 0f);
        tracker.Step(time: 0.05f, deltaTime: 0.05f);
        tracker.Clear();

        Assert.That(tracker.Value, Is.EqualTo(0f));
        Assert.That(tracker.Step(time: 0.06f, deltaTime: 0.01f), Is.EqualTo(0f));
    }
}
