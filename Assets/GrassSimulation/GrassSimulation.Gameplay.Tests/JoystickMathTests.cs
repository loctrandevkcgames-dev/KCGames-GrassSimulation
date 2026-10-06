using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class JoystickMathTests
{
    private static readonly Vector2 s_screen = new(x: 1080f, y: 1920f);
    private static readonly Rect s_lowerHalf = new(x: 0f, y: 0f, width: 1f, height: 0.5f);

    [TestCase(540f, 100f, true)]
    [TestCase(540f, 960f, true)]
    [TestCase(540f, 961f, false)]
    [TestCase(540f, 1800f, false)]
    [TestCase(0f, 0f, true)]
    [TestCase(1080f, 960f, true)]
    public void IsInRegion_UsesNormalizedRect(float x, float y, bool expected)
    {
        var result = JoystickMath.IsInRegion(new Vector2(x, y), s_screen, s_lowerHalf);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void IsInRegion_RejectsEmptyScreen()
    {
        Assert.That(JoystickMath.IsInRegion(Vector2.zero, Vector2.zero, s_lowerHalf), Is.False);
    }

    [Test]
    public void CanStartDrag_InsideRegionAndNotBlocked_IsTrue()
    {
        var result = JoystickMath.CanStartDrag(new Vector2(500f, 300f), s_screen, s_lowerHalf, _ => false);

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanStartDrag_WithoutBlocker_IsTrue()
    {
        var result = JoystickMath.CanStartDrag(
              position: new Vector2(500f, 300f)
            , screenSize: s_screen
            , region: s_lowerHalf
            , isBlocked: null
        );

        Assert.That(result, Is.True);
    }

    [Test]
    public void CanStartDrag_Blocked_IsFalse()
    {
        var result = JoystickMath.CanStartDrag(new Vector2(500f, 300f), s_screen, s_lowerHalf, _ => true);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CanStartDrag_OutsideRegion_DoesNotAskBlocker()
    {
        var asked = false;

        var result = JoystickMath.CanStartDrag(
              new Vector2(500f, 1500f)
            , s_screen
            , s_lowerHalf
            , _ => {
                asked = true;
                return false;
            }
        );

        Assert.That(result, Is.False);
        Assert.That(asked, Is.False);
    }

    [Test]
    public void CanStartDrag_PassesPositionToBlocker()
    {
        var position = new Vector2(123f, 456f);
        var received = Vector2.zero;

        JoystickMath.CanStartDrag(
              position
            , s_screen
            , s_lowerHalf
            , p => {
                received = p;
                return false;
            }
        );

        Assert.That(received, Is.EqualTo(position));
    }

    [Test]
    public void GetRadius_ScalesWithScreenWidth()
    {
        var radius = JoystickMath.GetRadius(
              referenceSize: 1080f
            , sizeFraction: JoystickMath.RADIUS_WIDTH_FRACTION
            , minRadius: JoystickMath.MIN_RADIUS
        );

        Assert.That(radius, Is.EqualTo(66f / 390f * 1080f).Within(0.01f));
    }

    [Test]
    public void GetRadius_NeverFallsBelowMinimum()
    {
        var radius = JoystickMath.GetRadius(
              referenceSize: 200f
            , sizeFraction: JoystickMath.RADIUS_WIDTH_FRACTION
            , minRadius: JoystickMath.MIN_RADIUS
        );

        Assert.That(radius, Is.EqualTo(JoystickMath.MIN_RADIUS));
    }

    [Test]
    public void GetInput_ScalesOffsetByRadius()
    {
        var input = JoystickMath.GetInput(
              origin: new Vector2(100f, 100f)
            , position: new Vector2(150f, 100f)
            , radius: 100f
        );

        Assert.That(input, Is.EqualTo(new Vector2(0.5f, 0f)));
    }

    [Test]
    public void GetInput_ClampsToUnitLength()
    {
        var input = JoystickMath.GetInput(
              origin: Vector2.zero
            , position: new Vector2(300f, 400f)
            , radius: 100f
        );

        Assert.That(input.magnitude, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(input.x, Is.EqualTo(0.6f).Within(1e-5f));
        Assert.That(input.y, Is.EqualTo(0.8f).Within(1e-5f));
    }

    [Test]
    public void GetInput_ZeroRadius_IsZero()
    {
        var input = JoystickMath.GetInput(
              origin: Vector2.zero
            , position: new Vector2(10f, 10f)
            , radius: 0f
        );

        Assert.That(input, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void ApplyDeadZone_ZeroesSmallInput()
    {
        Assert.That(JoystickMath.ApplyDeadZone(new Vector2(0.09f, 0f)), Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void ApplyDeadZone_KeepsInputAtThreshold()
    {
        var input = JoystickMath.ApplyDeadZone(new Vector2(JoystickMath.DEAD_ZONE, 0f));

        Assert.That(input, Is.EqualTo(new Vector2(JoystickMath.DEAD_ZONE, 0f)));
    }

    [Test]
    public void ApplyDeadZone_ClampsLargeInput()
    {
        var input = JoystickMath.ApplyDeadZone(new Vector2(3f, 4f));

        Assert.That(input.magnitude, Is.EqualTo(1f).Within(1e-5f));
    }
}
