using EncosyTower.Common;
using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class GameplayScreenFormatTests
{
    [TestCase(0f, "0:00")]
    [TestCase(-3f, "0:00")]
    [TestCase(0.2f, "0:01")]
    [TestCase(9f, "0:09")]
    [TestCase(72f, "1:12")]
    [TestCase(71.2f, "1:12")]
    [TestCase(600f, "10:00")]
    [TestCase(725f, "12:05")]
    public void FormatTimer_RoundsUpToWholeSeconds(float remaining, string expected)
    {
        Assert.That(GameplayScreenFormat.FormatTimer(remaining), Is.EqualTo(expected));
    }

    [Test]
    public void IsTimerWarning_TriggersAtTheConfiguredSecondsInclusive()
    {
        Assert.That(GameplayScreenFormat.IsTimerWarning(15f, warningSeconds: 15f), Is.True);
        Assert.That(GameplayScreenFormat.IsTimerWarning(15.01f, warningSeconds: 15f), Is.False);
        Assert.That(GameplayScreenFormat.IsTimerWarning(0f, warningSeconds: 15f), Is.True);
        Assert.That(GameplayScreenFormat.IsTimerWarning(20f, warningSeconds: 30f), Is.True);
    }

    [Test]
    public void FormatQuotaRemaining_ShowsHowManyAreLeftAndNeverGoesNegative()
    {
        Assert.That(GameplayScreenFormat.FormatQuotaRemaining(48, 70), Is.EqualTo("22"));
        Assert.That(GameplayScreenFormat.FormatQuotaRemaining(0, 70), Is.EqualTo("70"));
        Assert.That(GameplayScreenFormat.FormatQuotaRemaining(80, 70), Is.EqualTo("0"));
    }

    [Test]
    public void FormatHits_ShowsHitsLeftInFailModeAndTheCountInWarnMode()
    {
        Assert.That(GameplayScreenFormat.FormatHits(1, 3, isFailMode: true), Is.EqualTo("Còn 2 lỗi"));
        Assert.That(GameplayScreenFormat.FormatHits(2, 3, isFailMode: false), Is.EqualTo("2 lỗi"));
    }

    [Test]
    public void FormatQuotaProgress_ClampsProgressToAmount()
    {
        Assert.That(GameplayScreenFormat.FormatQuotaProgress(48, 70), Is.EqualTo("48 / 70"));
        Assert.That(GameplayScreenFormat.FormatQuotaProgress(30, 25), Is.EqualTo("25 / 25"));
    }

    [Test]
    public void GetQuotaFraction_IsClampedAndSafeForZeroAmount()
    {
        Assert.That(GameplayScreenFormat.GetQuotaFraction(5, 10), Is.EqualTo(0.5f));
        Assert.That(GameplayScreenFormat.GetQuotaFraction(30, 25), Is.EqualTo(1f));
        Assert.That(GameplayScreenFormat.GetQuotaFraction(0, 0), Is.EqualTo(1f));
    }

    [Test]
    public void FormatBonus_UsesLowercaseLabel()
    {
        Assert.That(
              GameplayScreenFormat.FormatBonus(PlantKind.ThickGrass, 12, 30)
            , Is.EqualTo("Phụ: cỏ dày 12 / 30")
        );
    }

    [Test]
    public void FormatHitsLeft_NeverGoesNegative()
    {
        Assert.That(GameplayScreenFormat.FormatHitsLeft(1, 3), Is.EqualTo("Còn 2 lỗi"));
        Assert.That(GameplayScreenFormat.FormatHitsLeft(5, 3), Is.EqualTo("Còn 0 lỗi"));
    }

    [Test]
    public void FormatXp_ShowsNextThresholdUnlessMaxTier()
    {
        Assert.That(GameplayScreenFormat.FormatXp(198, Option.Some(260)), Is.EqualTo("198 / 260 XP"));
        Assert.That(GameplayScreenFormat.FormatXp(900, Option.None), Is.EqualTo("900 XP"));
    }

    [Test]
    public void GetXpFraction_MeasuresProgressWithinCurrentTier()
    {
        Assert.That(GameplayScreenFormat.GetXpFraction(180, 100, Option.Some(260)), Is.EqualTo(0.5f).Within(1e-5f));
        Assert.That(GameplayScreenFormat.GetXpFraction(100, 100, Option.Some(260)), Is.Zero);
    }

    [Test]
    public void GetXpFraction_IsFullAtMaxTier()
    {
        Assert.That(GameplayScreenFormat.GetXpFraction(900, 600, Option.None), Is.EqualTo(1f));
    }

    [TestCase(0f, 0)]
    [TestCase(0.456f, 45)]
    [TestCase(0.999f, 99)]
    [TestCase(1f, 100)]
    [TestCase(1.4f, 100)]
    [TestCase(-0.2f, 0)]
    public void GetClearedPercent_FloorsAndClamps(float fraction, int expected)
    {
        Assert.That(GameplayScreenFormat.GetClearedPercent(fraction), Is.EqualTo(expected));
    }

    [Test]
    public void FormatCleared_ShowsThePercent()
    {
        Assert.That(GameplayScreenFormat.FormatCleared(percent: 63), Is.EqualTo("Đã dọn 63%"));
    }
}
