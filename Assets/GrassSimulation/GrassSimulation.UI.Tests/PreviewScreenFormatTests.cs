using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class PreviewScreenFormatTests
{
    [TestCase(0, "Màn 1")]
    [TestCase(3, "Màn 4")]
    public void FormatTitle_IsOneBased(int index, string expected)
    {
        Assert.That(PreviewScreenFormat.FormatTitle(index), Is.EqualTo(expected));
    }

    [TestCase(120f, "2:00")]
    [TestCase(90f, "1:30")]
    [TestCase(59.2f, "1:00")]
    public void FormatTimer_RoundsUpLikeTheGameplayTimer(float timeLimit, string expected)
    {
        Assert.That(PreviewScreenFormat.FormatTimer(timeLimit), Is.EqualTo(expected));
    }

    [Test]
    public void FormatQuotaAmount_ShowsTheAmount()
    {
        Assert.That(PreviewScreenFormat.FormatQuotaAmount(70), Is.EqualTo("70"));
    }

    [Test]
    public void FormatQuotaLabel_PrefixesBonusQuotasOnly()
    {
        var main = PreviewScreenFormat.FormatQuotaLabel(PlantKind.HarvestFlower, isBonus: false);
        var bonus = PreviewScreenFormat.FormatQuotaLabel(PlantKind.ThickGrass, isBonus: true);

        Assert.That(main, Is.EqualTo("Hoa đỏ"));
        Assert.That(bonus, Is.EqualTo("Phụ · Cỏ dày"));
    }

    [TestCase(1, "1 sao")]
    [TestCase(3, "3 sao")]
    public void FormatStarTitle_ShowsTheStarNumber(int star, string expected)
    {
        Assert.That(PreviewScreenFormat.FormatStarTitle(star), Is.EqualTo(expected));
    }

    [TestCase(120f, "Còn ≥ 0:24")]
    [TestCase(90f, "Còn ≥ 0:18")]
    [TestCase(118f, "Còn ≥ 0:24")]
    public void FormatTimeRule_RoundsTheThresholdUp(float timeLimit, string expected)
    {
        Assert.That(PreviewScreenFormat.FormatTimeRule(timeLimit), Is.EqualTo(expected));
    }

    [Test]
    public void FormatTimeRule_ShowsTheSameThresholdAsTheResultPopup()
    {
        const float TIME_LIMIT = 118f;

        var resultText = ResultPopupFormat.FormatTimeStar(remaining: 30f, timeLimit: TIME_LIMIT);
        var ruleText = PreviewScreenFormat.FormatTimeRule(TIME_LIMIT);

        Assert.That(resultText, Does.EndWith("≥ 0:24)"));
        Assert.That(ruleText, Does.EndWith("0:24"));
    }

    [Test]
    public void FormatCleanRule_MentionsTheBonusOnlyWhenTheLevelHasOne()
    {
        Assert.That(PreviewScreenFormat.FormatCleanRule(hasBonus: true), Is.EqualTo("Không lỗi + phụ"));
        Assert.That(PreviewScreenFormat.FormatCleanRule(hasBonus: false), Is.EqualTo("Không lỗi"));
    }
}
