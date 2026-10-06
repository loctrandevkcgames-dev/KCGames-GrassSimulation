using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.UI.Tests;

public sealed class JoystickLayoutTests
{
    [Test]
    public void GetUnitsPerPixel_DividesCanvasByScreenWidth()
    {
        Assert.That(JoystickLayout.GetUnitsPerPixel(canvasWidth: 1080f, screenWidth: 540f), Is.EqualTo(2f));
    }

    [Test]
    public void GetUnitsPerPixel_EmptyScreen_IsZero()
    {
        Assert.That(JoystickLayout.GetUnitsPerPixel(canvasWidth: 1080f, screenWidth: 0f), Is.EqualTo(0f));
    }

    [Test]
    public void ToCanvasPosition_ScalesEachAxis()
    {
        var position = JoystickLayout.ToCanvasPosition(
              screenPosition: new Vector2(270f, 480f)
            , screenSize: new Vector2(540f, 960f)
            , canvasSize: new Vector2(1080f, 1920f)
        );

        Assert.That(position, Is.EqualTo(new Vector2(540f, 960f)));
    }

    [Test]
    public void ToCanvasPosition_EmptyScreen_IsZero()
    {
        var position = JoystickLayout.ToCanvasPosition(
              screenPosition: new Vector2(10f, 10f)
            , screenSize: Vector2.zero
            , canvasSize: new Vector2(1080f, 1920f)
        );

        Assert.That(position, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void GetBaseSize_IsDiameterInCanvasUnits()
    {
        Assert.That(JoystickLayout.GetBaseSize(radiusPixels: 100f, unitsPerPixel: 1f), Is.EqualTo(200f));
    }

    [Test]
    public void GetKnobSize_FollowsDesignRatio()
    {
        var size = JoystickLayout.GetKnobSize(radiusPixels: 66f, unitsPerPixel: 1f);

        Assert.That(size, Is.EqualTo(JoystickLayout.KNOB_DESIGN_SIZE).Within(0.01f));
    }

    [Test]
    public void GetKnobOffset_IsInputTimesRadiusInCanvasUnits()
    {
        var offset = JoystickLayout.GetKnobOffset(
              input: new Vector2(0.5f, -1f)
            , radiusPixels: 90f
            , unitsPerPixel: 2f
        );

        Assert.That(offset, Is.EqualTo(new Vector2(90f, -180f)));
    }
}
