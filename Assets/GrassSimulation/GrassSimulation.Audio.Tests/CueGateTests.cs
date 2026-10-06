using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Audio.Tests;

public sealed class CueGateTests
{
    private static readonly Vector2 s_noPitch = Vector2.one;

    [Test]
    public void TryPick_RejectsInsideTheCooldown()
    {
        var gate = new CueGate(seed: 7u);

        Assert.That(gate.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, s_noPitch, 0.08f, 1f, out _), Is.True);
        Assert.That(gate.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, s_noPitch, 0.08f, 1.05f, out _), Is.False);
        Assert.That(gate.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, s_noPitch, 0.08f, 1.09f, out _), Is.True);
    }

    [Test]
    public void TryPick_KeepsCooldownsIndependentPerSound()
    {
        var gate = new CueGate(seed: 7u);

        Assert.That(gate.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, s_noPitch, 1f, 1f, out _), Is.True);
        Assert.That(gate.TryPick(SoundId.BushTrim, 3, CueVariantOrder.RandomNoRepeat, s_noPitch, 1f, 1f, out _), Is.True);
    }

    [Test]
    public void TryPick_RandomNoRepeatNeverRepeatsTheLastVariant()
    {
        var gate = new CueGate(seed: 12345u);
        var previous = -1;

        for (var i = 0; i < 200; i++)
        {
            Assert.That(
                  gate.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, s_noPitch, 0f, i, out var pick)
                , Is.True
            );
            Assert.That(pick.Variant, Is.InRange(0, 3));
            Assert.That(pick.Variant, Is.Not.EqualTo(previous));
            previous = pick.Variant;
        }
    }

    [Test]
    public void TryPick_SingleClipAlwaysPicksZero()
    {
        var gate = new CueGate(seed: 3u);

        for (var i = 0; i < 5; i++)
        {
            Assert.That(gate.TryPick(SoundId.QuotaComplete, 1, CueVariantOrder.RandomNoRepeat, s_noPitch, 0f, i, out var pick), Is.True);
            Assert.That(pick.Variant, Is.EqualTo(0));
        }
    }

    [Test]
    public void TryPick_SequenceCyclesThroughTheClips()
    {
        var gate = new CueGate(seed: 3u);
        var expected = new[] { 0, 1, 2, 0, 1 };

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.That(gate.TryPick(SoundId.Tap, 3, CueVariantOrder.Sequence, s_noPitch, 0f, i, out var pick), Is.True);
            Assert.That(pick.Variant, Is.EqualTo(expected[i]));
        }
    }

    [Test]
    public void TryPick_IndexedClampsTheIndex()
    {
        var gate = new CueGate(seed: 3u);

        Assert.That(gate.TryPick(SoundId.Star, 3, index: 1, s_noPitch, 0f, 0f, out var middle), Is.True);
        Assert.That(middle.Variant, Is.EqualTo(1));
        Assert.That(gate.TryPick(SoundId.Star, 3, index: 9, s_noPitch, 0f, 1f, out var high), Is.True);
        Assert.That(high.Variant, Is.EqualTo(2));
        Assert.That(gate.TryPick(SoundId.Star, 3, index: -4, s_noPitch, 0f, 2f, out var low), Is.True);
        Assert.That(low.Variant, Is.EqualTo(0));
    }

    [Test]
    public void TryPick_KeepsThePitchInsideTheRange()
    {
        var gate = new CueGate(seed: 99u);
        var range = new Vector2(0.92f, 1.08f);

        for (var i = 0; i < 100; i++)
        {
            Assert.That(gate.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, range, 0f, i, out var pick), Is.True);
            Assert.That(pick.Pitch, Is.InRange(0.92f, 1.08f));
        }
    }

    [Test]
    public void TryPick_IsDeterministicForTheSameSeed()
    {
        var first = new CueGate(seed: 42u);
        var second = new CueGate(seed: 42u);
        var range = new Vector2(0.9f, 1.1f);

        for (var i = 0; i < 30; i++)
        {
            first.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, range, 0f, i, out var a);
            second.TryPick(SoundId.GrassSnip, 4, CueVariantOrder.RandomNoRepeat, range, 0f, i, out var b);

            Assert.That(a, Is.EqualTo(b));
        }
    }

    [Test]
    public void TryPick_FailsWithoutClips()
    {
        var gate = new CueGate(seed: 1u);

        Assert.That(gate.TryPick(SoundId.Tap, 0, CueVariantOrder.RandomNoRepeat, s_noPitch, 0f, 0f, out _), Is.False);
        Assert.That(gate.TryPick(SoundId.Tap, 0, index: 0, s_noPitch, 0f, 0f, out _), Is.False);
    }

    [Test]
    public void Clear_ResetsTheCooldowns()
    {
        var gate = new CueGate(seed: 1u);

        gate.TryPick(SoundId.Tap, 3, CueVariantOrder.RandomNoRepeat, s_noPitch, 10f, 0f, out _);
        gate.Clear();

        Assert.That(gate.TryPick(SoundId.Tap, 3, CueVariantOrder.RandomNoRepeat, s_noPitch, 10f, 1f, out _), Is.True);
    }
}
