using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class CameraPoseTests
{
    private static readonly CameraPose s_from = new(Focus: Vector3.zero, OrthoSize: 4f);
    private static readonly CameraPose s_to = new(Focus: new Vector3(x: 2f, y: 0f, z: -6f), OrthoSize: 10f);

    [Test]
    public void Lerp_ReturnsEndpoints()
    {
        Assert.That(CameraPose.Lerp(in s_from, in s_to, t: 0f), Is.EqualTo(s_from));
        Assert.That(CameraPose.Lerp(in s_from, in s_to, t: 1f), Is.EqualTo(s_to));
    }

    [Test]
    public void Lerp_Midpoint_BlendsFocusAndSize()
    {
        var mid = CameraPose.Lerp(in s_from, in s_to, t: 0.5f);

        Assert.That(mid.Focus, Is.EqualTo(new Vector3(x: 1f, y: 0f, z: -3f)));
        Assert.That(mid.OrthoSize, Is.EqualTo(7f));
    }

    [Test]
    public void Lerp_ClampsT()
    {
        Assert.That(CameraPose.Lerp(in s_from, in s_to, t: 2f), Is.EqualTo(s_to));
        Assert.That(CameraPose.Lerp(in s_from, in s_to, t: -1f), Is.EqualTo(s_from));
    }
}
