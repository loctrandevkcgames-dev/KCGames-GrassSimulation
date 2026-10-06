using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Audio.Tests;

public sealed class CutLayerMixTests
{
    private static readonly CutLayerThresholds s_thresholds = new(light: 10f, medium: 40f, dense: 100f);

    [Test]
    public void Evaluate_IsSilentWithoutCuts()
    {
        Assert.That(CutLayerMix.Evaluate(0f, in s_thresholds), Is.EqualTo(default(CutLayerWeights)));
    }

    [Test]
    public void Evaluate_ReachesEachLayerAtItsThreshold()
    {
        Assert.That(CutLayerMix.Evaluate(10f, in s_thresholds).Light, Is.EqualTo(1f).Within(1e-4f));
        Assert.That(CutLayerMix.Evaluate(40f, in s_thresholds).Medium, Is.EqualTo(1f).Within(1e-4f));
        Assert.That(CutLayerMix.Evaluate(100f, in s_thresholds), Is.EqualTo(new CutLayerWeights(0f, 0f, 1f)));
        Assert.That(CutLayerMix.Evaluate(500f, in s_thresholds), Is.EqualTo(new CutLayerWeights(0f, 0f, 1f)));
    }

    [Test]
    public void Evaluate_KeepsEqualPowerBetweenLayers()
    {
        for (var density = 10f; density < 100f; density += 5f)
        {
            var weights = CutLayerMix.Evaluate(density, in s_thresholds);
            var power = weights.Light * weights.Light + weights.Medium * weights.Medium + weights.Dense * weights.Dense;

            Assert.That(power, Is.EqualTo(1f).Within(1e-3f), $"density {density}");
        }
    }

    [Test]
    public void Pitch_FollowsDensityInsideTheRange()
    {
        var range = new Vector2(0.95f, 1.05f);

        Assert.That(CutLayerMix.Pitch(0f, range), Is.EqualTo(0.95f).Within(1e-5f));
        Assert.That(CutLayerMix.Pitch(0.5f, range), Is.EqualTo(1f).Within(1e-5f));
        Assert.That(CutLayerMix.Pitch(1f, range), Is.EqualTo(1.05f).Within(1e-5f));
        Assert.That(CutLayerMix.Pitch(9f, range), Is.EqualTo(1.05f).Within(1e-5f));
    }
}
