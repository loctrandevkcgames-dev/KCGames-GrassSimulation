using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class CutMathTests
{
    private const float STANDARD_RADIUS = 0.65f;
    private const float TOLERANCE = 0.001f;

    [TestCase(0.25f, 0.12f, 15f)]
    [TestCase(0.25f, 0.18f, 10f)]
    [TestCase(0.25f, 0.3f, 6f)]
    [TestCase(0.4f, 0.45f, 4.667f)]
    [TestCase(0.4f, 0.55f, 3.818f)]
    [TestCase(0.6f, 0.65f, 3.846f)]
    [TestCase(0.6f, 0.9f, 2.778f)]
    [TestCase(0.6f, 2f, 1.25f)]
    [TestCase(1.25f, 2.5f, 1.52f)]
    public void CenterSpeed_MatchesTheBalancePlantTable(float zoneRadius, float toughness, float expected)
    {
        var speed = CutMath.CenterSpeed(STANDARD_RADIUS, zoneRadius, cuttingPower: 1f, toughness);

        Assert.That(speed, Is.EqualTo(expected).Within(TOLERANCE));
    }

    [Test]
    public void CenterSpeed_ScalesWithCuttingPower()
    {
        var weak = CutMath.CenterSpeed(STANDARD_RADIUS, 0.6f, cuttingPower: 1f, toughness: 0.9f);
        var strong = CutMath.CenterSpeed(STANDARD_RADIUS, 0.6f, cuttingPower: 1.3f, toughness: 0.9f);

        Assert.That(strong, Is.EqualTo(weak * 1.3f).Within(TOLERANCE));
    }

    [TestCase(0.65f, 4f, 0.25f, 0.12f, 1.735f)]
    [TestCase(0.65f, 4f, 0.25f, 0.18f, 1.65f)]
    [TestCase(1.23f, 3.6f, 0.6f, 0.9f, 1.702f)]
    [TestCase(0.95f, 4f, 0.6f, 0.9f, 0f)]
    public void Swath_MatchesTheBalanceSwathTable(
          float bladeRadius
        , float speed
        , float zoneRadius
        , float toughness
        , float expected
    )
    {
        var cutTime = CutMath.CutTime(toughness, cuttingPower: 1f);
        var swath = CutMath.Swath(bladeRadius, zoneRadius, speed, cutTime);

        Assert.That(swath, Is.EqualTo(expected).Within(TOLERANCE));
    }

    [Test]
    public void Swath_IsZeroWhenThePlantNeedsMoreTimeThanTheBladeTouchesIt()
    {
        var swath = CutMath.Swath(bladeRadius: 0.65f, zoneRadius: 0.6f, speed: 4f, cutTime: 2.5f);

        Assert.That(swath, Is.Zero);
    }

    [Test]
    public void ClosestPoint_ClampsToTheSegment()
    {
        var from = new Vector2(0f, 0f);
        var to = new Vector2(4f, 0f);

        Assert.That(CutMath.ClosestPoint(from, to, new Vector2(2f, 3f)), Is.EqualTo(new Vector2(2f, 0f)));
        Assert.That(CutMath.ClosestPoint(from, to, new Vector2(-2f, 3f)), Is.EqualTo(from));
        Assert.That(CutMath.ClosestPoint(from, to, new Vector2(9f, 3f)), Is.EqualTo(to));
        Assert.That(CutMath.ClosestPoint(from, from, new Vector2(9f, 3f)), Is.EqualTo(from));
    }

    [Test]
    public void ContactCoverage_IsTheFractionOfTheSegmentInsideTheReach()
    {
        var from = new Vector2(0f, 0f);
        var to = new Vector2(10f, 0f);

        var coverage = CutMath.ContactCoverage(from, to, new Vector2(5f, 0f), radius: 1f);

        Assert.That(coverage, Is.EqualTo(0.2f).Within(TOLERANCE));
        Assert.That(CutMath.ContactCoverage(from, to, new Vector2(5f, 2f), radius: 1f), Is.Zero);
    }
}
