using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class ProtectedRuleTests
{
    private const float RETRIGGER = 1f;

    [Test]
    public void FirstTouch_IsAHit()
    {
        var rule = new ProtectedRule(RETRIGGER, bedCount: 1);

        Assert.That(rule.Touch(bed: 0, time: 0f), Is.True);
        Assert.That(rule.Hits, Is.EqualTo(1));
    }

    [Test]
    public void ContinuousContact_CountsOnce()
    {
        var rule = new ProtectedRule(RETRIGGER, bedCount: 1);

        for (var i = 0; i < 100; i++)
        {
            rule.Touch(bed: 0, time: i * 0.1f);
        }

        Assert.That(rule.Hits, Is.EqualTo(1));
    }

    [Test]
    public void ReEntryAfterTheRetriggerTime_IsANewHit()
    {
        var rule = new ProtectedRule(RETRIGGER, bedCount: 1);

        rule.Touch(bed: 0, time: 0f);

        Assert.That(rule.Touch(bed: 0, time: 0.9f), Is.False);
        Assert.That(rule.Touch(bed: 0, time: 2.5f), Is.True);
        Assert.That(rule.Hits, Is.EqualTo(2));
    }

    [Test]
    public void TwoBedsTouchedWithinTheRetriggerTime_CountTwoHits()
    {
        var rule = new ProtectedRule(RETRIGGER, bedCount: 2);

        Assert.That(rule.Touch(bed: 0, time: 0f), Is.True);
        Assert.That(rule.Touch(bed: 1, time: 0.1f), Is.True);
        Assert.That(rule.Hits, Is.EqualTo(2));
    }

    [Test]
    public void EachBedKeepsItsOwnRetriggerClock()
    {
        var rule = new ProtectedRule(RETRIGGER, bedCount: 2);

        rule.Touch(bed: 0, time: 0f);
        rule.Touch(bed: 1, time: 0.5f);

        Assert.That(rule.Touch(bed: 0, time: 0.6f), Is.False);
        Assert.That(rule.Touch(bed: 1, time: 0.7f), Is.False);
        Assert.That(rule.Hits, Is.EqualTo(2));
    }

    [Test]
    public void BedBeyondTheKnownCount_IsTrackedToo()
    {
        var rule = new ProtectedRule(RETRIGGER, bedCount: 0);

        Assert.That(rule.Touch(bed: 3, time: 0f), Is.True);
        Assert.That(rule.Touch(bed: 3, time: 0.2f), Is.False);
        Assert.That(rule.Touch(bed: 0, time: 0.2f), Is.True);
    }

    [Test]
    public void Reset_ClearsHitsAndClocks()
    {
        var rule = new ProtectedRule(RETRIGGER, bedCount: 1);

        rule.Touch(bed: 0, time: 0f);
        rule.Reset();

        Assert.That(rule.Hits, Is.Zero);
        Assert.That(rule.Touch(bed: 0, time: 0.1f), Is.True);
    }
}
