using NUnit.Framework;

namespace GrassSimulation.Audio.Tests;

public sealed class DuckEnvelopeTests
{
    private const float DUCK_DB = -6f;
    private const float ATTACK = 0.05f;
    private const float RELEASE = 0.5f;

    [Test]
    public void Gain_IsUnityWithoutATrigger()
    {
        var envelope = new DuckEnvelope();

        Assert.That(envelope.Gain(0f, DUCK_DB, ATTACK, RELEASE), Is.EqualTo(1f));
        Assert.That(envelope.Gain(1f, DUCK_DB, ATTACK, RELEASE), Is.EqualTo(1f));
    }

    [Test]
    public void Gain_DucksAfterTheAttackAndHoldsForTheDuration()
    {
        var envelope = new DuckEnvelope();
        var target = AudioVolume.FromDecibels(DUCK_DB);

        envelope.Gain(0f, DUCK_DB, ATTACK, RELEASE);
        envelope.Trigger(now: 0f, hold: 1.5f);

        Assert.That(envelope.Gain(0.1f, DUCK_DB, ATTACK, RELEASE), Is.EqualTo(target).Within(1e-4f));
        Assert.That(envelope.Gain(1.4f, DUCK_DB, ATTACK, RELEASE), Is.EqualTo(target).Within(1e-4f));
    }

    [Test]
    public void Gain_ReleasesBackToUnity()
    {
        var envelope = new DuckEnvelope();

        envelope.Gain(0f, DUCK_DB, ATTACK, RELEASE);
        envelope.Trigger(now: 0f, hold: 1.5f);
        envelope.Gain(0.1f, DUCK_DB, ATTACK, RELEASE);
        envelope.Gain(1.5f, DUCK_DB, ATTACK, RELEASE);

        Assert.That(envelope.Gain(3f, DUCK_DB, ATTACK, RELEASE), Is.EqualTo(1f).Within(1e-4f));
    }

    [Test]
    public void Trigger_ExtendsTheHoldWhenRetriggered()
    {
        var envelope = new DuckEnvelope();
        var target = AudioVolume.FromDecibels(DUCK_DB);

        envelope.Gain(0f, DUCK_DB, ATTACK, RELEASE);
        envelope.Trigger(now: 0f, hold: 1.5f);
        envelope.Gain(1f, DUCK_DB, ATTACK, RELEASE);
        envelope.Trigger(now: 1f, hold: 1.5f);

        Assert.That(envelope.Gain(2.4f, DUCK_DB, ATTACK, RELEASE), Is.EqualTo(target).Within(1e-4f));
    }
}
