using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.UI.Tests;

public sealed class PreviewViewportTests
{
    private static readonly Vector2 s_screen = new(x: 1080f, y: 1920f);

    [Test]
    public void TryFromScreen_NormalisesRect()
    {
        var min = new Vector2(x: 54f, y: 768f);
        var max = new Vector2(x: 1026f, y: 1536f);

        var ok = PreviewViewport.TryFromScreen(min, max, s_screen, out var rect);

        Assert.That(ok, Is.True);
        Assert.That(rect.xMin, Is.EqualTo(0.05f).Within(1e-4f));
        Assert.That(rect.yMin, Is.EqualTo(0.4f).Within(1e-4f));
        Assert.That(rect.xMax, Is.EqualTo(0.95f).Within(1e-4f));
        Assert.That(rect.yMax, Is.EqualTo(0.8f).Within(1e-4f));
    }

    [Test]
    public void TryFromScreen_RejectsZeroScreen()
    {
        Assert.That(PreviewViewport.TryFromScreen(Vector2.zero, Vector2.one, Vector2.zero, out _), Is.False);
    }

    [Test]
    public void TryFromScreen_RejectsZeroSizeRect()
    {
        var point = new Vector2(x: 500f, y: 500f);

        Assert.That(PreviewViewport.TryFromScreen(point, point, s_screen, out _), Is.False);
    }

    [Test]
    public void TryFromScreen_ClampsSpill()
    {
        var min = new Vector2(x: -50f, y: -50f);
        var max = new Vector2(x: 2000f, y: 3000f);

        var ok = PreviewViewport.TryFromScreen(min, max, s_screen, out var rect);

        Assert.That(ok, Is.True);
        Assert.That(rect, Is.EqualTo(new Rect(x: 0f, y: 0f, width: 1f, height: 1f)));
    }

    [Test]
    public void IsSame_UsesTolerance()
    {
        var a = new Rect(x: 0.1f, y: 0.2f, width: 0.5f, height: 0.5f);
        var near = new Rect(x: 0.1005f, y: 0.2f, width: 0.5f, height: 0.5f);
        var far = new Rect(x: 0.102f, y: 0.2f, width: 0.5f, height: 0.5f);

        Assert.That(PreviewViewport.IsSame(a, near), Is.True);
        Assert.That(PreviewViewport.IsSame(a, far), Is.False);
    }
}
