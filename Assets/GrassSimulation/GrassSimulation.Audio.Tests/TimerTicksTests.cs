using NUnit.Framework;

namespace GrassSimulation.Audio.Tests;

public sealed class TimerTicksTests
{
    private const float WARNING = 15f;
    private const float ACCENT = 5f;

    [Test]
    public void Evaluate_TicksWhenEnteringTheWarningWindow()
    {
        Assert.That(TimerTicks.Evaluate(15.2f, 14.9f, WARNING, ACCENT), Is.EqualTo(TimerTick.Tick));
    }

    [Test]
    public void Evaluate_IsSilentBeforeTheWarningWindow()
    {
        Assert.That(TimerTicks.Evaluate(16f, 15.5f, WARNING, ACCENT), Is.EqualTo(TimerTick.None));
    }

    [Test]
    public void Evaluate_IsSilentInsideOneSecond()
    {
        Assert.That(TimerTicks.Evaluate(10.9f, 10.5f, WARNING, ACCENT), Is.EqualTo(TimerTick.None));
    }

    [Test]
    public void Evaluate_AccentsInTheLastFiveSeconds()
    {
        Assert.That(TimerTicks.Evaluate(5.1f, 4.9f, WARNING, ACCENT), Is.EqualTo(TimerTick.Accent));
    }

    [Test]
    public void Evaluate_ReturnsOneTickWhenAFrameSkipsSeveralSeconds()
    {
        Assert.That(TimerTicks.Evaluate(9f, 6f, WARNING, ACCENT), Is.EqualTo(TimerTick.Tick));
        Assert.That(TimerTicks.Evaluate(6f, 3f, WARNING, ACCENT), Is.EqualTo(TimerTick.Accent));
    }

    [Test]
    public void Evaluate_IgnoresAnUpwardReset()
    {
        Assert.That(TimerTicks.Evaluate(2f, 12f, WARNING, ACCENT), Is.EqualTo(TimerTick.None));
    }

    [Test]
    public void Evaluate_IgnoresReachingZero()
    {
        Assert.That(TimerTicks.Evaluate(0.4f, 0f, WARNING, ACCENT), Is.EqualTo(TimerTick.None));
    }
}
