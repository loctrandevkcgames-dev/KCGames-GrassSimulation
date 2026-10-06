using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelOutcomeTests
{
    [Test]
    public void TimeUp_IsTimeUpOnly()
    {
        LevelOutcome outcome = new LevelOutcome.TimeUp(RemainingQuota: 5);

        Assert.That(outcome.IsTimeUp, Is.True);
        Assert.That(outcome.IsTooManyProtectedHits, Is.False);
        Assert.That(outcome.TryGetProtectedHits(out var hits, out var limit), Is.False);
        Assert.That(hits, Is.Zero);
        Assert.That(limit, Is.Zero);
        Assert.That(outcome.IsSuccess, Is.False);
    }

    [Test]
    public void TooManyProtectedHits_IsProtectedFailureOnly()
    {
        LevelOutcome outcome = new LevelOutcome.TooManyProtectedHits(Hits: 4, Limit: 3);

        Assert.That(outcome.IsTooManyProtectedHits, Is.True);
        Assert.That(outcome.TryGetProtectedHits(out var hits, out var limit), Is.True);
        Assert.That(hits, Is.EqualTo(4));
        Assert.That(limit, Is.EqualTo(3));
        Assert.That(outcome.IsTimeUp, Is.False);
        Assert.That(outcome.IsSuccess, Is.False);
    }

    [Test]
    public void Success_IsNeitherFailureKind()
    {
        LevelOutcome outcome = new LevelOutcome.Success(Stars: StarFlags.Goal | StarFlags.Clean);

        Assert.That(outcome.IsSuccess, Is.True);
        Assert.That(outcome.IsTimeUp, Is.False);
        Assert.That(outcome.IsTooManyProtectedHits, Is.False);
    }

    [Test]
    public void Undefined_IsNoOutcome()
    {
        var outcome = default(LevelOutcome);

        Assert.That(outcome.IsSuccess, Is.False);
        Assert.That(outcome.IsTimeUp, Is.False);
        Assert.That(outcome.IsTooManyProtectedHits, Is.False);
    }
}
