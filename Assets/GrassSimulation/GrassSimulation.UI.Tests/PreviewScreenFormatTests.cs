using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class PreviewScreenFormatTests
{
    private const float STAR2_TIME_LEFT = 0.2f;

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
        Assert.That(PreviewScreenFormat.FormatTimer(isTimed: true, timeLimit), Is.EqualTo(expected));
    }

    [Test]
    public void FormatTimer_SaysUnlimitedWhenUntimed()
    {
        Assert.That(PreviewScreenFormat.FormatTimer(isTimed: false, timeLimit: 0f), Is.EqualTo("Không giới hạn"));
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

    [TestCase(120f, "Không lỗi, còn ≥ 0:24")]
    [TestCase(90f, "Không lỗi, còn ≥ 0:18")]
    [TestCase(118f, "Không lỗi, còn ≥ 0:24")]
    public void FormatCleanRule_RoundsTheThresholdUpWhenTimed(float timeLimit, string expected)
    {
        Assert.That(PreviewScreenFormat.FormatCleanRule(isTimed: true, timeLimit, STAR2_TIME_LEFT), Is.EqualTo(expected));
    }

    [Test]
    public void FormatCleanRule_OnlyMentionsProtectedFlowersWhenUntimed()
    {
        Assert.That(
              PreviewScreenFormat.FormatCleanRule(isTimed: false, timeLimit: 0f, STAR2_TIME_LEFT)
            , Is.EqualTo("Không lỗi")
        );
    }

    [Test]
    public void FormatCleanRule_ShowsTheSameThresholdAsTheResultPopup()
    {
        const float TIME_LIMIT = 118f;

        var snapshot = default(LevelSnapshot) with { IsTimed = true, TimeLimit = TIME_LIMIT, Star2TimeLeft = STAR2_TIME_LEFT };
        var resultText = ResultPopupFormat.FormatCleanStar(in snapshot, remaining: 30f);
        var ruleText = PreviewScreenFormat.FormatCleanRule(isTimed: true, TIME_LIMIT, STAR2_TIME_LEFT);

        Assert.That(resultText, Does.EndWith("≥ 0:24)"));
        Assert.That(ruleText, Does.EndWith("0:24"));
    }

    [Test]
    public void FormatSideRule_NamesTheSideQuotaOrTheSweepGoal()
    {
        Assert.That(PreviewScreenFormat.FormatSideRule(hasBonus: true), Is.EqualTo("Làm xong mục tiêu phụ"));
        Assert.That(PreviewScreenFormat.FormatSideRule(hasBonus: false), Is.EqualTo("Dọn ≥ 90% cây"));
    }
}
