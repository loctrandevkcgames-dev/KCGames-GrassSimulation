using System.Collections.Generic;
using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.Progression.Tests;

public sealed class RewardCalculatorTests
{
    private static readonly LevelId s_level = new("level-01");

    private List<string> _granted;
    private List<RewardGrant> _output;

    [SetUp]
    public void SetUp()
    {
        _granted = new List<string>();
        _output = new List<RewardGrant>();
    }

    [Test]
    public void FirstWinWithTwoStars_PaysOneHundredFifty()
    {
        RewardCalculator.Collect(s_level, isWin: true, stars: 2, _granted, _output);

        Assert.That(TotalCoins(), Is.EqualTo(150));
        Assert.That(_output.Count, Is.EqualTo(3));
    }

    [Test]
    public void ReplayWithThirdStar_PaysOnlyTheNewStar()
    {
        Settle(stars: 2);
        Settle(stars: 3);

        Assert.That(TotalCoins(), Is.EqualTo(25));
    }

    [Test]
    public void ReplayWithSameStars_PaysNothing()
    {
        Settle(stars: 2);
        Settle(stars: 2);

        Assert.That(_output, Is.Empty);
    }

    [Test]
    public void Loss_PaysNothing()
    {
        RewardCalculator.Collect(s_level, isWin: false, stars: 3, _granted, _output);

        Assert.That(_output, Is.Empty);
    }

    [Test]
    public void StarsAboveMaximum_AreClamped()
    {
        RewardCalculator.Collect(s_level, isWin: true, stars: 9, _granted, _output);

        Assert.That(TotalCoins(), Is.EqualTo(175));
        Assert.That(_output.Count, Is.EqualTo(1 + RewardRules.MAX_STARS));
    }

    [Test]
    public void NegativeStars_PayOnlyFirstWin()
    {
        RewardCalculator.Collect(s_level, isWin: true, stars: -4, _granted, _output);

        Assert.That(TotalCoins(), Is.EqualTo(RewardRules.FIRST_WIN_COINS));
        Assert.That(_output.Count, Is.EqualTo(1));
    }

    [Test]
    public void ZeroStars_PayOnlyFirstWin()
    {
        RewardCalculator.Collect(s_level, isWin: true, stars: 0, _granted, _output);

        Assert.That(TotalCoins(), Is.EqualTo(RewardRules.FIRST_WIN_COINS));
    }

    [Test]
    public void LedgerWrittenByAnEarlierBuild_IsStillHonored()
    {
        _granted.Add("first-win:level-01");
        _granted.Add("star:level-01:1");

        RewardCalculator.Collect(s_level, isWin: true, stars: 2, _granted, _output);

        Assert.That(_output.Count, Is.EqualTo(1));
        Assert.That(_output[0].Id.ToKey(), Is.EqualTo("star:level-01:2"));
    }

    [Test]
    public void OtherLevels_AreTrackedSeparately()
    {
        Settle(stars: 3);
        _output.Clear();

        RewardCalculator.Collect(new LevelId("level-02"), isWin: true, stars: 1, _granted, _output);

        Assert.That(TotalCoins(), Is.EqualTo(125));
    }

    [Test]
    public void RewardKeys_KeepTheLedgerFormat()
    {
        RewardId firstWin = new RewardId.FirstWin(s_level);
        RewardId star = new RewardId.Star(s_level, 2);

        Assert.That(firstWin.ToKey(), Is.EqualTo("first-win:level-01"));
        Assert.That(star.ToKey(), Is.EqualTo("star:level-01:2"));
    }

    private void Settle(int stars)
    {
        _output.Clear();
        RewardCalculator.Collect(s_level, isWin: true, stars, _granted, _output);

        var count = _output.Count;

        for (var i = 0; i < count; i++)
        {
            _granted.Add(_output[i].Id.ToKey());
        }
    }

    private int TotalCoins()
    {
        var total = 0;
        var count = _output.Count;

        for (var i = 0; i < count; i++)
        {
            total += _output[i].Coins;
        }

        return total;
    }
}
