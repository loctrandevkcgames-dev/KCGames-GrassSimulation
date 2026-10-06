using System.Collections.Generic;
using NUnit.Framework;

namespace GrassSimulation.Audio.Tests;

public sealed class CueSequenceSchedulerTests
{
    private const float STEP = 0.25f;

    [Test]
    public void TryTakeDue_ReleasesACueEveryStep()
    {
        var scheduler = new CueSequenceScheduler();

        scheduler.Schedule(SoundId.Star, count: 3, now: 10f, step: STEP);

        Assert.That(scheduler.TryTakeDue(10f, out var id, out var index), Is.True);
        Assert.That(id, Is.EqualTo(SoundId.Star));
        Assert.That(index, Is.EqualTo(0));
        Assert.That(scheduler.TryTakeDue(10.1f, out _, out _), Is.False);
        Assert.That(scheduler.TryTakeDue(10.25f, out _, out index), Is.True);
        Assert.That(index, Is.EqualTo(1));
        Assert.That(scheduler.TryTakeDue(10.5f, out _, out index), Is.True);
        Assert.That(index, Is.EqualTo(2));
        Assert.That(scheduler.PendingCount, Is.EqualTo(0));
    }

    [Test]
    public void Schedule_PlaysStarsBeforeCoinsInTheOrderRequested()
    {
        var scheduler = new CueSequenceScheduler();
        var order = new List<SoundId>();

        scheduler.Schedule(SoundId.Star, count: 3, now: 1f, step: STEP);
        scheduler.Schedule(SoundId.Coin, count: 3, now: 1f, step: STEP);

        while (scheduler.TryTakeDue(100f, out var id, out _))
        {
            order.Add(id);
        }

        Assert.That(order.Count, Is.EqualTo(6));
        Assert.That(order.GetRange(0, 3), Is.All.EqualTo(SoundId.Star));
        Assert.That(order.GetRange(3, 3), Is.All.EqualTo(SoundId.Coin));
    }

    [Test]
    public void Schedule_StartsAfterTheSequenceEnd()
    {
        var scheduler = new CueSequenceScheduler();

        scheduler.Schedule(SoundId.Star, count: 3, now: 0f, step: STEP);
        scheduler.Schedule(SoundId.Coin, count: 1, now: 0f, step: STEP);

        for (var i = 0; i < 3; i++)
        {
            scheduler.TryTakeDue(0.5f, out _, out _);
        }

        Assert.That(scheduler.TryTakeDue(0.74f, out _, out _), Is.False);
        Assert.That(scheduler.TryTakeDue(0.75f, out var id, out _), Is.True);
        Assert.That(id, Is.EqualTo(SoundId.Coin));
    }

    [Test]
    public void BlockUntil_DelaysTheNextSequence()
    {
        var scheduler = new CueSequenceScheduler();

        scheduler.BlockUntil(5f);
        scheduler.Schedule(SoundId.Star, count: 1, now: 1f, step: STEP);

        Assert.That(scheduler.TryTakeDue(4.9f, out _, out _), Is.False);
        Assert.That(scheduler.TryTakeDue(5f, out _, out _), Is.True);
    }

    [Test]
    public void Clear_DropsPendingCuesAndTheBlock()
    {
        var scheduler = new CueSequenceScheduler();

        scheduler.BlockUntil(5f);
        scheduler.Schedule(SoundId.Star, count: 3, now: 1f, step: STEP);
        scheduler.Clear();

        Assert.That(scheduler.PendingCount, Is.EqualTo(0));

        scheduler.Schedule(SoundId.Coin, count: 1, now: 2f, step: STEP);

        Assert.That(scheduler.TryTakeDue(2f, out var id, out _), Is.True);
        Assert.That(id, Is.EqualTo(SoundId.Coin));
    }
}
