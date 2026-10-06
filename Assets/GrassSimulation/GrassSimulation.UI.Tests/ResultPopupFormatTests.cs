using EncosyTower.Common;
using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class ResultPopupFormatTests
{
    private const float STAR2_TIME_LEFT = 0.2f;

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
    public void GetTimeThreshold_IsTheConfiguredFractionOfTheTimeLimit()
    {
        Assert.That(ResultPopupFormat.GetTimeThreshold(120f, STAR2_TIME_LEFT), Is.EqualTo(24f).Within(1e-4f));
    }

    [TestCase(120f, "0:24")]
    [TestCase(118f, "0:24")]
    [TestCase(100f, "0:20")]
    public void FormatThreshold_RoundsUpSoItNeverShowsLessThanTheRealThreshold(float timeLimit, string expected)
    {
        Assert.That(ResultPopupFormat.FormatThreshold(timeLimit, STAR2_TIME_LEFT), Is.EqualTo(expected));
    }

    [TestCase(24f, true)]
    [TestCase(23.9f, false)]
    [TestCase(60f, true)]
    public void HasTimeStar_IncludesTheThreshold(float remaining, bool expected)
    {
        Assert.That(ResultPopupFormat.HasTimeStar(remaining, 120f, STAR2_TIME_LEFT), Is.EqualTo(expected));
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
    public void FormatCleanStar_ShowsRemainingAndThresholdWhenTimed()
    {
        var snapshot = Timed();

        Assert.That(ResultPopupFormat.FormatCleanStar(in snapshot, remaining: 31f), Is.EqualTo("Không lỗi, còn 0:31 (≥ 0:24)"));
    }

    [Test]
    public void FormatCleanStar_FloorsTheRemainingTimeSoItNeverReachesTheThresholdEarly()
    {
        var snapshot = Timed();

        Assert.That(ResultPopupFormat.FormatCleanStar(in snapshot, remaining: 23.5f), Is.EqualTo("Không lỗi, còn 0:23 (≥ 0:24)"));
    }

    [Test]
    public void FormatCleanStar_OnlyMentionsProtectedFlowersWhenUntimed()
    {
        var snapshot = default(LevelSnapshot) with { IsTimed = false };

        Assert.That(ResultPopupFormat.FormatCleanStar(in snapshot, remaining: 0f), Is.EqualTo("Không lỗi"));
    }

    [Test]
    public void FormatSideStar_NamesTheSideQuota()
    {
        var bonus = new ResultBonus(First: s_thickGrass, AreAllMet: false);

        Assert.That(ResultPopupFormat.FormatSideStar(in bonus, clearedFraction: 0.5f), Is.EqualTo("Phụ: cỏ dày 30"));
    }

    [Test]
    public void FormatSideStar_FallsBackToTheSweepGoal()
    {
        var bonus = new ResultBonus(First: Option.None, AreAllMet: true);

        Assert.That(ResultPopupFormat.FormatSideStar(in bonus, clearedFraction: 0.82f), Is.EqualTo("Dọn 82% (≥ 90%)"));
    }

    [Test]
    public void HasStar_ChecksTheFlag()
    {
        var stars = StarFlags.Goal | StarFlags.Side;

        Assert.That(ResultPopupFormat.HasStar(stars, StarFlags.Goal), Is.True);
        Assert.That(ResultPopupFormat.HasStar(stars, StarFlags.Clean), Is.False);
        Assert.That(ResultPopupFormat.HasStar(stars, StarFlags.Side), Is.True);
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
    public void CreateCleanRow_ChecksOnlyWhenTheCleanStarIsEarned()
    {
        var snapshot = Timed();
        var met = ResultPopupFormat.CreateCleanRow(CreateResult(StarFlags.Goal | StarFlags.Clean, 31f), in snapshot);
        var unmet = ResultPopupFormat.CreateCleanRow(CreateResult(StarFlags.Goal, 10f), in snapshot);

        Assert.That(met.ShowCheck, Is.True);
        Assert.That(met.Label, Is.EqualTo("Không lỗi, còn 0:31 (≥ 0:24)"));
        Assert.That(unmet.ShowCheck, Is.False);
        Assert.That(unmet.Tone, Is.EqualTo(UiPalette.Muted));
    }

    [Test]
    public void CreateSideRow_ShowsBonusProgressWhenUnmet()
    {
        var snapshot = Timed();
        var bonus = new ResultBonus(First: s_thickGrass, AreAllMet: false);
        var row = ResultPopupFormat.CreateSideRow(CreateResult(StarFlags.Goal, 10f), in snapshot, in bonus);

        Assert.That(row.ShowCheck, Is.False);
        Assert.That(row.Value.TryGetValue(out var value), Is.True);
        Assert.That(value, Is.EqualTo("18 / 30"));
    }

    [Test]
    public void CreateSideRow_HasNoValueWithoutABonus()
    {
        var snapshot = Timed();
        var bonus = new ResultBonus(First: Option.None, AreAllMet: true);
        var unmet = ResultPopupFormat.CreateSideRow(CreateResult(StarFlags.Goal, 10f), in snapshot, in bonus);
        var met = ResultPopupFormat.CreateSideRow(CreateResult(StarFlags.Goal | StarFlags.Side, 10f), in snapshot, in bonus);

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

    private static LevelSnapshot Timed()
    {
        return default(LevelSnapshot) with {
            IsTimed = true,
            TimeLimit = 120f,
            Star2TimeLeft = STAR2_TIME_LEFT,
        };
    }

    private static LevelResult CreateResult(StarFlags stars, float remaining)
    {
        LevelOutcome outcome = new LevelOutcome.Success(stars);

        return new LevelResult(Outcome: outcome, RemainingTime: remaining, ProtectedHits: 0);
    }
}
