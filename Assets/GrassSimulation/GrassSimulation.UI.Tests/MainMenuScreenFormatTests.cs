using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class MainMenuScreenFormatTests
{
    [Test]
    public void FormatCoins_ShowsTheBalance()
    {
        Assert.That(MainMenuScreenFormat.FormatCoins(175), Is.EqualTo("175"));
    }

    [TestCase(10, 30)]
    [TestCase(0, 0)]
    public void GetMaxStars_IsThreePerLevel(int levelCount, int expected)
    {
        Assert.That(MainMenuScreenFormat.GetMaxStars(levelCount), Is.EqualTo(expected));
    }

    [Test]
    public void FormatProgress_ShowsLevelsAndStars()
    {
        var text = MainMenuScreenFormat.FormatProgress(completedCount: 3, levelCount: 10, totalStars: 7);

        Assert.That(text, Is.EqualTo("3 / 10 màn · 7 / 30 sao"));
    }

    [TestCase(3, 10, 0.3f)]
    [TestCase(0, 10, 0f)]
    [TestCase(10, 10, 1f)]
    [TestCase(12, 10, 1f)]
    [TestCase(0, 0, 0f)]
    public void GetProgressFraction_IsClampedAndSafeForEmptyCatalogs(int completed, int count, float expected)
    {
        Assert.That(MainMenuScreenFormat.GetProgressFraction(completed, count), Is.EqualTo(expected).Within(1e-5f));
    }

    [TestCase(3, 10, false)]
    [TestCase(10, 10, true)]
    [TestCase(0, 0, false)]
    public void IsGardenComplete_RequiresAtLeastOneLevel(int completed, int count, bool expected)
    {
        Assert.That(MainMenuScreenFormat.IsGardenComplete(completed, count), Is.EqualTo(expected));
    }

    [Test]
    public void FormatNextHeader_SwitchesWhenEveryLevelIsDone()
    {
        var inProgress = MainMenuScreenFormat.FormatNextHeader(completedCount: 3, levelCount: 10);
        var done = MainMenuScreenFormat.FormatNextHeader(completedCount: 10, levelCount: 10);

        Assert.That(inProgress, Is.EqualTo("Màn tiếp theo"));
        Assert.That(done, Is.EqualTo("Màn cuối"));
    }

    [TestCase(0, "Màn 1")]
    [TestCase(3, "Màn 4")]
    public void FormatLevelNumber_IsOneBased(int index, string expected)
    {
        Assert.That(MainMenuScreenFormat.FormatLevelNumber(index), Is.EqualTo(expected));
    }

    [TestCase(PlantKind.HarvestFlower, 70, "70 hoa đỏ")]
    [TestCase(PlantKind.LowBush, 25, "25 bụi thấp")]
    public void FormatQuotaChip_ShowsAmountAndLowercaseLabel(PlantKind kind, int amount, string expected)
    {
        Assert.That(MainMenuScreenFormat.FormatQuotaChip(kind, amount), Is.EqualTo(expected));
    }
}
