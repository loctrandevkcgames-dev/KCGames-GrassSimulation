using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class OrthoFramingTests
{
    private const float EPSILON = 1e-3f;

    private static readonly Rect s_full = new(x: 0f, y: 0f, width: 1f, height: 1f);

    [Test]
    public void TryFit_SquareTopDown_FitsHalfExtent()
    {
        var ground = new Rect(x: -5f, y: -5f, width: 10f, height: 10f);

        var pose = Fit(ground, pitch: 90f, aspect: 1f, viewport: s_full);

        Assert.That(pose.OrthoSize, Is.EqualTo(5f).Within(EPSILON));
        Assert.That(pose.Focus.x, Is.EqualTo(0f).Within(EPSILON));
        Assert.That(pose.Focus.z, Is.EqualTo(0f).Within(EPSILON));
    }

    [Test]
    public void TryFit_PortraitAspect_IsWidthLimited()
    {
        var ground = new Rect(x: -9f, y: -7f, width: 18f, height: 14f);

        var pose = Fit(ground, pitch: 60f, aspect: 0.5625f, viewport: s_full);

        Assert.That(pose.OrthoSize, Is.EqualTo(16f).Within(EPSILON));
        Assert.That(pose.Focus.x, Is.EqualTo(0f).Within(EPSILON));
        Assert.That(pose.Focus.z, Is.EqualTo(0f).Within(EPSILON));
    }

    [Test]
    public void TryFit_UpperHalfViewport_ShiftsFocusAway()
    {
        var ground = new Rect(x: -5f, y: -5f, width: 10f, height: 10f);
        var viewport = new Rect(x: 0f, y: 0.5f, width: 1f, height: 0.5f);

        var pose = Fit(ground, pitch: 90f, aspect: 1f, viewport: viewport);

        Assert.That(pose.OrthoSize, Is.EqualTo(10f).Within(EPSILON));
        Assert.That(pose.Focus.z, Is.EqualTo(-5f).Within(EPSILON));
    }

    [Test]
    public void TryFit_OffCentreGround_FocusFollowsCentre()
    {
        var ground = new Rect(x: 1f, y: -4f, width: 4f, height: 4f);

        var pose = Fit(ground, pitch: 90f, aspect: 1f, viewport: s_full);

        Assert.That(pose.Focus.x, Is.EqualTo(3f).Within(EPSILON));
        Assert.That(pose.Focus.z, Is.EqualTo(-2f).Within(EPSILON));
    }

    [Test]
    public void TryFit_HeightAllowance_ShiftsFocusAndGrowsSize()
    {
        var ground = new Rect(x: -5f, y: -5f, width: 10f, height: 10f);
        var sin = Mathf.Sin(60f * Mathf.Deg2Rad);
        var cos = Mathf.Cos(60f * Mathf.Deg2Rad);

        var flat = Fit(ground, pitch: 60f, aspect: 2f, viewport: s_full);
        var tall = Fit(ground, pitch: 60f, aspect: 2f, viewport: s_full, height: 2f);

        Assert.That(tall.OrthoSize, Is.GreaterThan(flat.OrthoSize));
        Assert.That(tall.Focus.z - flat.Focus.z, Is.EqualTo(2f * cos / 2f / sin).Within(EPSILON));
    }

    [Test]
    public void TryFit_ProjectedCorners_StayInsideViewportAndTouchAnEdge(
          [Values(55f, 60f, 65f)] float pitch
        , [Values(0.5625f, 1f, 1.7778f)] float aspect
        , [Values(0, 1, 2)] int viewportIndex
    )
    {
        var viewports = new[]
        {
            s_full,
            new Rect(x: 0.05f, y: 0.4f, width: 0.9f, height: 0.4f),
            new Rect(x: 0.2f, y: 0.1f, width: 0.5f, height: 0.6f),
        };
        var viewport = viewports[viewportIndex];
        var ground = new Rect(x: -9f, y: -7f, width: 18f, height: 14f);
        const float MARGIN = 0.5f;
        const float HEIGHT = 2f;

        var pose = Fit(ground, pitch, aspect, viewport, MARGIN, HEIGHT);

        var sin = Mathf.Sin(pitch * Mathf.Deg2Rad);
        var cos = Mathf.Cos(pitch * Mathf.Deg2Rad);
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);

        for (var i = 0; i < 4; i++)
        {
            var x = (i & 1) == 0 ? ground.xMin - MARGIN : ground.xMax + MARGIN;
            var isFar = (i & 2) != 0;
            var z = isFar ? ground.yMax + MARGIN : ground.yMin - MARGIN;
            var y = isFar ? HEIGHT : 0f;

            var vx = 0.5f + (x - pose.Focus.x) / (2f * pose.OrthoSize * aspect);
            var vy = 0.5f + ((z - pose.Focus.z) * sin + y * cos) / (2f * pose.OrthoSize);

            min = Vector2.Min(min, new Vector2(vx, vy));
            max = Vector2.Max(max, new Vector2(vx, vy));
        }

        Assert.That(min.x, Is.GreaterThanOrEqualTo(viewport.xMin - EPSILON));
        Assert.That(min.y, Is.GreaterThanOrEqualTo(viewport.yMin - EPSILON));
        Assert.That(max.x, Is.LessThanOrEqualTo(viewport.xMax + EPSILON));
        Assert.That(max.y, Is.LessThanOrEqualTo(viewport.yMax + EPSILON));

        var widthTight = Mathf.Abs(max.x - min.x - viewport.width) < EPSILON;
        var heightTight = Mathf.Abs(max.y - min.y - viewport.height) < EPSILON;

        Assert.That(widthTight || heightTight, Is.True);
    }

    [TestCase(0f, 1f, 1f, 1f)]
    [TestCase(60f, 0f, 1f, 1f)]
    [TestCase(60f, 1f, 0f, 1f)]
    [TestCase(60f, 1f, 1f, 0f)]
    public void TryFit_RejectsDegenerateInput(float pitch, float aspect, float width, float height)
    {
        var ground = new Rect(x: -5f, y: -5f, width: 10f, height: 10f);
        var viewport = new Rect(x: 0f, y: 0f, width: width, height: height);

        var fits = OrthoFraming.TryFit(
              groundXZ: ground
            , pitchDegrees: pitch
            , aspect: aspect
            , viewport: viewport
            , margin: 0f
            , heightAllowance: 0f
            , pose: out _
        );

        Assert.That(fits, Is.False);
    }

    private static CameraPose Fit(
          Rect ground
        , float pitch
        , float aspect
        , Rect viewport
        , float margin = 0f
        , float height = 0f
    )
    {
        var fits = OrthoFraming.TryFit(
              groundXZ: ground
            , pitchDegrees: pitch
            , aspect: aspect
            , viewport: viewport
            , margin: margin
            , heightAllowance: height
            , pose: out var pose
        );

        Assert.That(fits, Is.True);
        return pose;
    }
}
