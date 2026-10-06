using System.Collections.Generic;
using System.IO;
using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.Editor.Tests;

public sealed class LevelSpecTableTests
{
    [Test]
    public void Parse_ReadsAllFiftyRowsOfTheShippedTable()
    {
        var specs = Load();

        Assert.That(specs, Has.Count.EqualTo(50));
        Assert.That(specs[0].Id, Is.EqualTo("L01"));
        Assert.That(specs[49].Id, Is.EqualTo("L50"));
    }

    [Test]
    public void Parse_ReadsTheFieldsOfLevelSix()
    {
        var spec = Load()[5];

        Assert.That(spec.Width, Is.EqualTo(18));
        Assert.That(spec.Length, Is.EqualTo(14));
        Assert.That(spec.MaxTier, Is.EqualTo(2));
        Assert.That(spec.Type, Is.EqualTo(LevelType.Normal));
        Assert.That(spec.GetCount(PlantKind.Grass), Is.EqualTo(100));
        Assert.That(spec.GetCount(PlantKind.BushLow), Is.EqualTo(25));
        Assert.That(spec.IsTimed, Is.True);
        Assert.That(spec.SuggestedTimer, Is.EqualTo(95f));
        Assert.That(spec.Unlock, Is.EqualTo(new UnlockSettings { Kind = UnlockKind.TurboBooster, Amount = 3 }));
        Assert.That(spec.Quotas, Has.Length.EqualTo(2));
        Assert.That(spec.Quotas[0], Is.EqualTo(new QuotaSettings { Kind = PlantKind.HarvestFlower, Amount = 60 }));
        Assert.That(spec.Quotas[1], Is.EqualTo(new QuotaSettings { Kind = PlantKind.BushLow, Amount = 20 }));
    }

    [Test]
    public void Parse_MarksTheSideQuotaAsBonus()
    {
        var spec = Load()[6];
        var side = new QuotaSettings { Kind = PlantKind.ThickGrass, Amount = 18, IsBonus = true };

        Assert.That(spec.Quotas[^1], Is.EqualTo(side));
    }

    [Test]
    public void Parse_ReadsUntimedTutorialAndRelaxLevels()
    {
        var specs = Load();

        Assert.That(specs[0].Type, Is.EqualTo(LevelType.Tutorial));
        Assert.That(specs[0].IsTimed, Is.False);
        Assert.That(specs[27].Type, Is.EqualTo(LevelType.Relax));
        Assert.That(specs[29].Type, Is.EqualTo(LevelType.Hard));
    }

    [Test]
    public void Parse_ReadsTheUnlocks()
    {
        var specs = Load();
        var extraTime = new UnlockSettings { Kind = UnlockKind.ExtraTimeBooster, Amount = 3 };

        Assert.That(specs[3].Unlock, Is.EqualTo(extraTime));
        Assert.That(specs[9].Unlock.Kind, Is.EqualTo(UnlockKind.WideMachine));
        Assert.That(specs[1].Unlock.Kind, Is.EqualTo(UnlockKind.None));
    }

    [Test]
    public void Parse_RejectsAnUnknownPlantName()
    {
        var csv = "id,type,q1_kind,q1_amount,grass,timer\nL01,Thường,Nấm,5,10,Có\n";

        Assert.That(LevelSpecTable.Parse(csv).IsError, Is.True);
    }

    [Test]
    public void Parse_KeepsQuotedCommasInsideAField()
    {
        var specs = Load();

        Assert.That(specs[4].Decision, Does.Contain("Hoa cho quota, cỏ cho XP"));
        Assert.That(specs[9].Name, Is.EqualTo("Sân rộng, luống hẹp"));
    }

    private static List<LevelSpec> Load()
    {
        var result = LevelSpecTable.Parse(File.ReadAllText(LevelPipeline.SPEC_PATH));

        Assert.That(result.TryGetValue(out var specs), Is.True);
        return specs;
    }
}
