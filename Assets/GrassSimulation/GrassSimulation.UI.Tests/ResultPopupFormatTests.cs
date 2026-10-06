using EncosyTower.Common;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class ResultPopupFormatTests
{
    private static readonly QuotaSnapshot s_thickGrass = new(
          Kind: PlantKind.ThickGrass
        , Amount: 30
        , Progress: 18
        , IsBonus: true
        , IsMet: false
    );

    private static readonly QuotaSnapshot s_flowers = new(
          Kind: PlantKind.HarvestFlower
        , Amount: 70
        , Progress: 70
        , IsBonus: false
        , IsMet: true
    );

    [TestCase(0, 3, true)]
    [TestCase(1, 3, true)]
    [TestCase(2, 3, false)]
    [TestCase(0, 1, false)]
    public void HasNextLevel_ComparesTheIndexWithTheCount(int index, int count, bool expected)
    {
        Assert.That(ResultPopupFormat.HasNextLevel(index, count), Is.EqualTo(expected));
    }

    [Test]
    public void GetTimeThreshold_IsTwentyPercentOfTheTimeLimit()
    {
        Assert.That(ResultPopupFormat.GetTimeThreshold(120f), Is.EqualTo(24f).Within(1e-4f));
    }

    [Test]
    public void FormatTimeStar_ShowsRemainingAndThreshold()
    {
        var text = ResultPopupFormat.FormatTimeStar(remaining: 31f, timeLimit: 120f);

        Assert.That(text, Is.EqualTo("Còn 0:31 (≥ 0:24)"));
    }

    [Test]
    public void FormatTimeStar_FloorsTheRemainingTimeSoItNeverReachesTheThresholdEarly()
    {
        var text = ResultPopupFormat.FormatTimeStar(remaining: 23.5f, timeLimit: 120f);

        Assert.That(text, Is.EqualTo("Còn 0:23 (≥ 0:24)"));
        Assert.That(ResultPopupFormat.HasTimeStar(remaining: 23.5f, timeLimit: 120f), Is.False);
        Assert.That(ResultPopupFormat.CreateTimeRow(remaining: 23.5f, timeLimit: 120f).ShowCheck, Is.False);
    }

    [TestCase(24f, true)]
    [TestCase(23.9f, false)]
    [TestCase(60f, true)]
    public void HasTimeStar_IncludesTheThreshold(float remaining, bool expected)
    {
        Assert.That(ResultPopupFormat.HasTimeStar(remaining, timeLimit: 120f), Is.EqualTo(expected));
    }

    [Test]
    public void GetBonus_IsEmptyAndMetWithoutBonusQuotas()
    {
        var snapshot = default(LevelSnapshot) with { QuotaCount = 1, Quota0 = s_flowers };
        var bonus = ResultPopupFormat.GetBonus(in snapshot);

        Assert.That(bonus.First.HasValue, Is.False);
        Assert.That(bonus.AreAllMet, Is.True);
    }

    [Test]
    public void GetBonus_ReportsTheFirstBonusAndRequiresEveryBonusToBeMet()
    {
        var met = s_thickGrass with { Progress = 30, IsMet = true };
        var allMet = default(LevelSnapshot) with { QuotaCount = 3, Quota0 = s_flowers, Quota1 = met, Quota2 = met };
        var oneOpen = allMet with { Quota2 = s_thickGrass };

        var metBonus = ResultPopupFormat.GetBonus(in allMet);
        var openBonus = ResultPopupFormat.GetBonus(in oneOpen);

        Assert.That(metBonus.AreAllMet, Is.True);
        Assert.That(openBonus.AreAllMet, Is.False);
        Assert.That(openBonus.First.TryGetValue(out var first), Is.True);
        Assert.That(first, Is.EqualTo(met));
    }

    [Test]
    public void FormatCleanStar_NamesTheBonusKindAndAmount()
    {
        var bonus = new ResultBonus(First: s_thickGrass, AreAllMet: false);

        Assert.That(ResultPopupFormat.FormatCleanStar(in bonus), Is.EqualTo("Không lỗi + phụ cỏ dày 30"));
    }

    [Test]
    public void FormatCleanStar_OmitsTheBonusWhenThereIsNone()
    {
        var bonus = new ResultBonus(First: Option.None, AreAllMet: true);

        Assert.That(ResultPopupFormat.FormatCleanStar(in bonus), Is.EqualTo("Không lỗi"));
    }

    [Test]
    public void HasCleanStar_NeedsNoHitsAndEveryBonusMet()
    {
        var met = new ResultBonus(First: s_thickGrass, AreAllMet: true);
        var open = new ResultBonus(First: s_thickGrass, AreAllMet: false);
        var none = new ResultBonus(First: Option.None, AreAllMet: true);

        Assert.That(ResultPopupFormat.HasCleanStar(protectedHits: 0, in none), Is.True);
        Assert.That(ResultPopupFormat.HasCleanStar(protectedHits: 0, in met), Is.True);
        Assert.That(ResultPopupFormat.HasCleanStar(protectedHits: 0, in open), Is.False);
        Assert.That(ResultPopupFormat.HasCleanStar(protectedHits: 1, in met), Is.False);
        Assert.That(ResultPopupFormat.HasCleanStar(protectedHits: 1, in none), Is.False);
    }

    [Test]
    public void CreateGoalRow_IsCheckedWithoutValue()
    {
        var row = ResultPopupFormat.CreateGoalRow();

        Assert.That(row.Label, Is.EqualTo("Đạt mục tiêu"));
        Assert.That(row.Value.HasValue, Is.False);
        Assert.That(row.ShowCheck, Is.True);
    }

    [Test]
    public void CreateTimeRow_ChecksOnlyWhenTheThresholdIsReached()
    {
        var met = ResultPopupFormat.CreateTimeRow(remaining: 31f, timeLimit: 120f);
        var unmet = ResultPopupFormat.CreateTimeRow(remaining: 10f, timeLimit: 120f);

        Assert.That(met.ShowCheck, Is.True);
        Assert.That(met.Label, Is.EqualTo("Còn 0:31 (≥ 0:24)"));
        Assert.That(unmet.ShowCheck, Is.False);
        Assert.That(unmet.Tone, Is.EqualTo(UiPalette.Muted));
    }

    [Test]
    public void CreateCleanRow_ShowsBonusProgressWhenUnmet()
    {
        var bonus = new ResultBonus(First: s_thickGrass, AreAllMet: false);
        var row = ResultPopupFormat.CreateCleanRow(protectedHits: 0, in bonus);

        Assert.That(row.ShowCheck, Is.False);
        Assert.That(row.Value.TryGetValue(out var value), Is.True);
        Assert.That(value, Is.EqualTo("18 / 30"));
    }

    [Test]
    public void CreateCleanRow_HasNoValueWithoutABonus()
    {
        var bonus = new ResultBonus(First: Option.None, AreAllMet: true);
        var unmet = ResultPopupFormat.CreateCleanRow(protectedHits: 2, in bonus);
        var met = ResultPopupFormat.CreateCleanRow(protectedHits: 0, in bonus);

        Assert.That(unmet.ShowCheck, Is.False);
        Assert.That(unmet.Value.HasValue, Is.False);
        Assert.That(met.ShowCheck, Is.True);
    }

    [Test]
    public void CreateQuotaRow_ShowsTheMissingAmountWhenUnmet()
    {
        var quota = new QuotaSnapshot(
              Kind: PlantKind.HarvestFlower
            , Amount: 70
            , Progress: 48
            , IsBonus: false
            , IsMet: false
        );

        var row = ResultPopupFormat.CreateQuotaRow(in quota);

        Assert.That(row.Label, Is.EqualTo("Hoa đỏ"));
        Assert.That(row.Value.TryGetValue(out var value), Is.True);
        Assert.That(value, Is.EqualTo("48 / 70 · thiếu 22"));
        Assert.That(row.ShowCheck, Is.False);
    }

    [Test]
    public void CreateQuotaRow_ChecksAMetQuota()
    {
        var quota = new QuotaSnapshot(
              Kind: PlantKind.LowBush
            , Amount: 25
            , Progress: 27
            , IsBonus: false
            , IsMet: true
        );

        var row = ResultPopupFormat.CreateQuotaRow(in quota);

        Assert.That(row.Label, Is.EqualTo("Bụi thấp"));
        Assert.That(row.Value.TryGetValue(out var value), Is.True);
        Assert.That(value, Is.EqualTo("25 / 25"));
        Assert.That(row.ShowCheck, Is.True);
        Assert.That(row.Tone, Is.EqualTo(UiPalette.Primary));
    }

    [Test]
    public void FormatCoinBreakdown_ListsTheFirstWinAndTheNewStars()
    {
        var settlement = CreateSettlement(coins: 150, firstWinCoins: 100, newStars: 2, isFirst: true);

        Assert.That(
              ResultPopupFormat.FormatCoinBreakdown(in settlement)
            , Is.EqualTo("Thắng lần đầu +100\n2 sao mới +50")
        );
    }

    [Test]
    public void FormatCoinBreakdown_ListsOnlyTheFirstWinWhenNoStarIsNew()
    {
        var settlement = CreateSettlement(coins: 100, firstWinCoins: 100, newStars: 0, isFirst: true);

        Assert.That(ResultPopupFormat.FormatCoinBreakdown(in settlement), Is.EqualTo("Thắng lần đầu +100"));
    }

    [Test]
    public void FormatCoinBreakdown_ListsOnlyNewStarsOnAReplay()
    {
        var settlement = CreateSettlement(coins: 25, firstWinCoins: 0, newStars: 1, isFirst: false);

        Assert.That(ResultPopupFormat.FormatCoinBreakdown(in settlement), Is.EqualTo("1 sao mới +25"));
    }

    [Test]
    public void FormatCoinBreakdown_SaysNoRewardWhenNothingWasGranted()
    {
        var settlement = CreateSettlement(coins: 0, firstWinCoins: 0, newStars: 0, isFirst: false);

        Assert.That(ResultPopupFormat.FormatCoinBreakdown(in settlement), Is.EqualTo("Không có thưởng mới"));
    }

    [Test]
    public void FormatCoinTotal_PrefixesAPlus()
    {
        Assert.That(ResultPopupFormat.FormatCoinTotal(150), Is.EqualTo("+150"));
    }

    [Test]
    public void FailureVisuals_UseTheClockForTimeUp()
    {
        LevelOutcome outcome = new LevelOutcome.TimeUp(RemainingQuota: 22);

        var visual = ResultFailureVisuals.Get(in outcome);

        Assert.That(visual.Icon, Is.EqualTo(ResultFailureIcon.Clock));
        Assert.That(visual.Title, Is.EqualTo("Hết giờ"));
        Assert.That(visual.Reason, Is.EqualTo("Chưa đủ mục tiêu khi hết thời gian."));
        Assert.That(visual.Tile, Is.EqualTo(UiPalette.TimeUpFill));
        Assert.That(visual.IconTint, Is.EqualTo(UiPalette.TimeUp));
    }

    [Test]
    public void FailureVisuals_UseTheShieldAndThePayloadForProtectedHits()
    {
        LevelOutcome outcome = new LevelOutcome.TooManyProtectedHits(Hits: 4, Limit: 3);

        var visual = ResultFailureVisuals.Get(in outcome);

        Assert.That(visual.Icon, Is.EqualTo(ResultFailureIcon.ShieldX));
        Assert.That(visual.Title, Is.EqualTo("Chạm hoa bảo vệ"));
        Assert.That(visual.Reason, Is.EqualTo("Lưỡi cắt chạm luống hoa bảo vệ 4 lần, vượt giới hạn 3."));
        Assert.That(visual.Tile, Is.EqualTo(UiPalette.ProtectedFill));
        Assert.That(visual.IconTint, Is.EqualTo(UiPalette.Protected));
    }

    private static LevelSettlement CreateSettlement(int coins, int firstWinCoins, int newStars, bool isFirst)
    {
        return new LevelSettlement(
              CoinsGranted: coins
            , FirstWinCoins: firstWinCoins
            , NewStars: newStars
            , Stars: newStars
            , IsNewBest: newStars > 0
            , IsFirstCompletion: isFirst
        );
    }
}
