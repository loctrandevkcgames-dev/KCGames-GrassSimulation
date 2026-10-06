using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class LevelValidatorTests
{
    private PlantDefinition[] _plants;

    [SetUp]
    public void SetUp()
    {
        _plants = LevelValidatorFixture.Plants();
    }

    [Test]
    public void Validate_AcceptsTheBaselineLevel()
    {
        var layout = LevelValidatorFixture.Layout();

        Assert.That(Validate(layout, LevelValidatorFixture.Spec(layout)), Is.Empty);
    }

    [Test]
    public void Validate_RejectsPlacedCountsThatDifferFromTheSpec()
    {
        var layout = LevelValidatorFixture.Layout();
        var spec = LevelValidatorFixture.Spec(layout);

        spec.PlantCounts[(int)PlantKind.Grass] += 1;

        AssertHasIssue(layout, spec, LevelRule.Counts);
    }

    [Test]
    public void Validate_RejectsLegacyFieldKinds()
    {
        var layout = LevelValidatorFixture.Layout();

        layout.Kinds[0] = PlantKind.LowBush;

        AssertHasIssue(layout, LevelRule.Counts);
    }

    [Test]
    public void Validate_RejectsBedsThatDifferFromTheSpec()
    {
        var layout = LevelValidatorFixture.Layout();
        var spec = LevelValidatorFixture.Spec(layout) with { BedCount = 2 };

        AssertHasIssue(layout, spec, LevelRule.Counts);
    }

    [Test]
    public void Validate_RejectsAQuotaWithoutTheSurplus()
    {
        var layout = LevelValidatorFixture.Layout() with {
            Quotas = new[] { LevelValidatorFixture.Quota(PlantKind.HarvestFlower, 22) },
        };

        AssertHasIssue(layout, LevelRule.Quota);
    }

    [Test]
    public void Validate_RejectsAQuotaAboveTheLevelTier()
    {
        var layout = LevelValidatorFixture.Layout();

        layout = layout with { Quotas = new[] { LevelValidatorFixture.Quota(PlantKind.ThickGrass, 1) } };
        layout.Kinds[0] = PlantKind.ThickGrass;
        layout.Kinds[1] = PlantKind.ThickGrass;

        AssertHasIssue(layout, LevelRule.Quota);
    }

    [Test]
    public void Validate_RejectsTotalXpThatReachesTheNextTier()
    {
        var layout = LevelValidatorFixture.Layout();

        LevelValidatorFixture.Fill(layout.Kinds, new RectInt(0, 6, 16, 3), PlantKind.Grass);

        AssertHasIssue(layout, LevelRule.Xp);
    }

    [Test]
    public void Validate_RejectsTooLittleXpToReachTheMaxTier()
    {
        var layout = LevelValidatorFixture.Layout() with { MaxTier = 2 };

        AssertHasIssue(layout, LevelRule.Xp);
    }

    [Test]
    public void Validate_RejectsTierFourPlantsThatGiveXp()
    {
        var layout = LevelValidatorFixture.Layout() with {
            Plants = new[] { new PlantPlacement { Kind = PlantKind.FruitTree, Position = new Vector2(6f, 6f) } },
        };

        _plants[4].Xp = 5;

        AssertHasIssue(layout, LevelRule.Xp);
    }

    [Test]
    public void Validate_RejectsPlantsTheBladeCannotReach()
    {
        var layout = LevelValidatorFixture.Layout() with {
            Obstacles = new[] {
                LevelValidatorFixture.Fence(2.5f, 7.6f, 4.2f),
                LevelValidatorFixture.Fence(2.5f, 4.4f, 4.2f),
                LevelValidatorFixture.Fence(0.4f, 6f, 3.2f, yaw: 90f),
                LevelValidatorFixture.Fence(4.6f, 6f, 3.2f, yaw: 90f),
            },
        };

        AssertHasIssue(layout, LevelRule.Reach);
    }

    [Test]
    public void Validate_RejectsAMainPathNarrowerThanTheMachine()
    {
        var layout = LevelValidatorFixture.Layout() with {
            Obstacles = new[] {
                LevelValidatorFixture.Fence(1.765f, 4f, 3.53f),
                LevelValidatorFixture.Fence(6.235f, 4f, 3.53f),
            },
        };

        var issues = Validate(layout, LevelValidatorFixture.Spec(layout));

        Assert.That(issues, Has.Some.Matches(IsRule(LevelRule.PathWidth)));
        Assert.That(issues, Has.None.Matches(IsRule(LevelRule.Reach)));
    }

    [Test]
    public void Validate_RejectsABedThatForcesTheBladeToTouchIt()
    {
        var layout = LevelValidatorFixture.Layout();
        var bed = new RectInt(0, 8, 14, 2);

        LevelValidatorFixture.Fill(layout.Kinds, bed, PlantKind.ProtectedFlower);
        layout = layout with { Beds = new[] { bed } };

        var issues = Validate(layout, LevelValidatorFixture.Spec(layout));

        Assert.That(issues, Has.Some.Matches(IsRule(LevelRule.ProtectedClearance)));
        Assert.That(issues, Has.None.Matches(IsRule(LevelRule.PathWidth)));
    }

    [Test]
    public void Validate_AcceptsABedWithAWideEnoughLane()
    {
        var layout = LevelValidatorFixture.Layout();
        var bed = new RectInt(0, 8, 8, 2);

        LevelValidatorFixture.Fill(layout.Kinds, bed, PlantKind.ProtectedFlower);
        layout = layout with { Beds = new[] { bed } };

        Assert.That(Validate(layout, LevelValidatorFixture.Spec(layout)), Is.Empty);
    }

    [Test]
    public void Validate_RejectsTooShortATimer()
    {
        var layout = LevelValidatorFixture.Layout() with { TimeLimit = 30f };

        AssertHasIssue(layout, LevelRule.Timer);
    }

    [Test]
    public void Validate_RejectsATimerThatDiffersFromTheSpec()
    {
        var layout = LevelValidatorFixture.Layout();
        var spec = LevelValidatorFixture.Spec(layout) with { SuggestedTimer = 90f };

        AssertHasIssue(layout, spec, LevelRule.Timer);
    }

    [TestCase(LevelType.Tutorial)]
    [TestCase(LevelType.Relax)]
    public void Validate_RejectsATimerOnAnUntimedLevelType(LevelType type)
    {
        var layout = LevelValidatorFixture.Layout() with { Type = type };

        AssertHasIssue(layout, LevelRule.Timer);
    }

    [Test]
    public void Validate_AcceptsUntimedTutorialLevels()
    {
        var layout = LevelValidatorFixture.Layout() with { Type = LevelType.Tutorial, TimeLimit = 0f };

        Assert.That(Validate(layout, LevelValidatorFixture.Spec(layout)), Is.Empty);
    }

    [Test]
    public void Validate_RejectsASpawnInsideAnObstacle()
    {
        var layout = LevelValidatorFixture.Layout() with {
            Obstacles = new[] { LevelValidatorFixture.Rock(6.5f, 0.5f) },
        };

        AssertHasIssue(layout, LevelRule.Spawn);
    }

    [Test]
    public void Validate_RejectsASpawnOnAPlant()
    {
        var layout = LevelValidatorFixture.Layout();

        layout.Kinds[1 * LevelValidatorFixture.CELLS + 13] = PlantKind.Grass;

        AssertHasIssue(layout, LevelRule.Spawn);
    }

    [Test]
    public void Validate_RejectsASpawnOutsideTheMap()
    {
        var layout = LevelValidatorFixture.Layout() with { Spawn = new Vector2(20f, 20f) };

        AssertHasIssue(layout, LevelRule.Spawn);
    }

    private static Predicate<LevelIssue> IsRule(LevelRule rule)
        => issue => issue.Rule == rule;

    private void AssertHasIssue(LevelLayout layout, LevelRule rule)
        => AssertHasIssue(layout, LevelValidatorFixture.Spec(layout), rule);

    private void AssertHasIssue(LevelLayout layout, LevelSpec spec, LevelRule rule)
        => Assert.That(Validate(layout, spec), Has.Some.Matches(IsRule(rule)));

    private List<LevelIssue> Validate(LevelLayout layout, LevelSpec spec)
        => LevelValidator.Validate(layout, spec, _plants, in LevelValidationRules.Default);
}
