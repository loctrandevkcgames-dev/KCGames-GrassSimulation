using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelPreviewTests
{
    private TestAssets _assets;

    [SetUp]
    public void SetUp()
    {
        _assets = new TestAssets();
    }

    [TearDown]
    public void TearDown()
    {
        _assets.Dispose();
    }

    [Test]
    public void From_ReportsLevelIdIndexTimeAndQuotas()
    {
        var level = _assets.CreateLevel(
              timeLimit: 90f
            , Quota(kind: PlantKind.HarvestFlower, amount: 70)
            , Quota(kind: PlantKind.ThickGrass, amount: 30, isBonus: true)
        );

        var preview = LevelPreview.From(level, levelIndex: 3);

        Assert.That(preview.Level, Is.EqualTo(new LevelId("level-00")));
        Assert.That(preview.LevelIndex, Is.EqualTo(3));
        Assert.That(preview.TimeLimit, Is.EqualTo(90f));
        Assert.That(preview.QuotaCount, Is.EqualTo(2));
        Assert.That(preview.GetQuota(0), Is.EqualTo(Quota(kind: PlantKind.HarvestFlower, amount: 70)));
        Assert.That(preview.GetQuota(1), Is.EqualTo(Quota(kind: PlantKind.ThickGrass, amount: 30, isBonus: true)));
        Assert.That(preview.GetQuota(2), Is.EqualTo(default(QuotaSettings)));
    }

    [Test]
    public void From_CapsTheQuotaCount()
    {
        var level = _assets.CreateLevel(
              timeLimit: 30f
            , Quota(kind: PlantKind.Grass, amount: 1)
            , Quota(kind: PlantKind.Grass, amount: 2)
            , Quota(kind: PlantKind.Grass, amount: 3)
            , Quota(kind: PlantKind.Grass, amount: 4)
            , Quota(kind: PlantKind.Grass, amount: 5)
        );

        var preview = LevelPreview.From(level, levelIndex: 0);

        Assert.That(preview.QuotaCount, Is.EqualTo(LevelPreview.MAX_QUOTAS));
        Assert.That(preview.GetQuota(3).Amount, Is.EqualTo(4));
    }

    private static QuotaSettings Quota(PlantKind kind, int amount, bool isBonus = false)
        => new() { Kind = kind, Amount = amount, IsBonus = isBonus };
}
