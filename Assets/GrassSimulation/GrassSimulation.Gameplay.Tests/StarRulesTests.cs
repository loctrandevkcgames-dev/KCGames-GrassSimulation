using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class StarRulesTests
{
    private const float TIME_LIMIT = 100f;
    private const float STAR2_TIME_LEFT = 0.2f;

    [Test]
    public void Loss_EarnsNothing()
    {
        var stars = Evaluate(isWin: false, remaining: 50f, hits: 0, isSideMet: true);

        Assert.That(stars, Is.EqualTo(StarFlags.None));
    }

    [Test]
    public void Win_AlwaysEarnsTheGoalStar()
    {
        var stars = Evaluate(isWin: true, remaining: 0f, hits: 5, isSideMet: false);

        Assert.That(stars, Is.EqualTo(StarFlags.Goal));
    }

    [Test]
    public void TimedWin_NeedsNoHitsAndTimeLeftForTheCleanStar()
    {
        Assert.That(
              Evaluate(isWin: true, remaining: 20.1f, hits: 0, isSideMet: false)
            , Is.EqualTo(StarFlags.Goal | StarFlags.Clean)
        );
        Assert.That(Evaluate(isWin: true, remaining: 19.9f, hits: 0, isSideMet: false), Is.EqualTo(StarFlags.Goal));
        Assert.That(Evaluate(isWin: true, remaining: 90f, hits: 1, isSideMet: false), Is.EqualTo(StarFlags.Goal));
    }

    [Test]
    public void UntimedWin_ChecksProtectedHitsOnly()
    {
        Assert.That(
              Evaluate(isWin: true, remaining: 0f, hits: 0, isSideMet: false, isTimed: false)
            , Is.EqualTo(StarFlags.Goal | StarFlags.Clean)
        );
        Assert.That(
              Evaluate(isWin: true, remaining: 0f, hits: 2, isSideMet: false, isTimed: false)
            , Is.EqualTo(StarFlags.Goal)
        );
    }

    [Test]
    public void SideStar_IsIndependentOfTheCleanStar()
    {
        var stars = Evaluate(isWin: true, remaining: 0f, hits: 3, isSideMet: true);

        Assert.That(stars, Is.EqualTo(StarFlags.Goal | StarFlags.Side));
    }

    [Test]
    public void AllConditions_EarnThreeStars()
    {
        var stars = Evaluate(isWin: true, remaining: 60f, hits: 0, isSideMet: true);

        Assert.That(stars, Is.EqualTo(StarFlags.Goal | StarFlags.Clean | StarFlags.Side));
        Assert.That(StarRules.Count(stars), Is.EqualTo(3));
    }

    [Test]
    public void AssistedWin_EarnsOnlyTheGoalStar()
    {
        var stars = Evaluate(isWin: true, remaining: 60f, hits: 0, isSideMet: true, isAssisted: true);

        Assert.That(stars, Is.EqualTo(StarFlags.Goal));
    }

    [Test]
    public void Count_CountsSetFlags()
    {
        Assert.That(StarRules.Count(StarFlags.None), Is.Zero);
        Assert.That(StarRules.Count(StarFlags.Side), Is.EqualTo(1));
        Assert.That(StarRules.Count(StarFlags.Goal | StarFlags.Side), Is.EqualTo(2));
    }

    private static StarFlags Evaluate(
          bool isWin
        , float remaining
        , int hits
        , bool isSideMet
        , bool isTimed = true
        , bool isAssisted = false
    )
    {
        return StarRules.Evaluate(
              isWin: isWin
            , isAssisted: isAssisted
            , isTimed: isTimed
            , remainingTime: remaining
            , timeLimit: TIME_LIMIT
            , star2TimeLeft: STAR2_TIME_LEFT
            , protectedHits: hits
            , isSideMet: isSideMet
        );
    }
}
