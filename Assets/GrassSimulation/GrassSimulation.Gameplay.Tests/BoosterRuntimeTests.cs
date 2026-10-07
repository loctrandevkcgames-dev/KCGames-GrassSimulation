using NUnit.Framework;

namespace GrassSimulation.Gameplay.Tests;

public sealed class BoosterRuntimeTests
{
    private BoosterRuntime _runtime;

    [SetUp]
    public void SetUp()
    {
        _runtime = new BoosterRuntime(BoosterValues.Default);
    }

    [Test]
    public void Activate_WithoutEquipping_IsRejectedAndConsumesNothing()
    {
        var activated = _runtime.TryActivate(BoosterKind.Turbo);

        Assert.That(activated.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterError.NotEquipped notEquipped), Is.True);
        Assert.That(notEquipped.Kind, Is.EqualTo(BoosterKind.Turbo));
        Assert.That(_runtime.IsUsed(BoosterKind.Turbo), Is.False);
        Assert.That(_runtime.GetState(BoosterKind.Turbo), Is.EqualTo(BoosterSlotState.NotEquipped));
    }

    [Test]
    public void EquippedBooster_IsReadyUntilActivated()
    {
        _runtime.SetEquipped(BoosterKind.Turbo, isEquipped: true);

        Assert.That(_runtime.GetState(BoosterKind.Turbo), Is.EqualTo(BoosterSlotState.Ready));
        Assert.That(_runtime.GetState(BoosterKind.ExtraTime), Is.EqualTo(BoosterSlotState.NotEquipped));
    }

    [Test]
    public void Turbo_RunsForItsDurationThenEndsAndStaysUsed()
    {
        _runtime.SetEquipped(BoosterKind.Turbo, isEquipped: true);

        Assert.That(_runtime.TryActivate(BoosterKind.Turbo).IsSuccess, Is.True);
        Assert.That(_runtime.GetState(BoosterKind.Turbo), Is.EqualTo(BoosterSlotState.Running));
        Assert.That(_runtime.GetRemaining(BoosterKind.Turbo), Is.EqualTo(8f));

        Assert.That(_runtime.Tick(7.9f), Is.Zero);
        Assert.That(_runtime.IsRunning(BoosterKind.Turbo), Is.True);

        Assert.That(_runtime.Tick(0.2f), Is.EqualTo(1));
        Assert.That(_runtime.GetEnded(0), Is.EqualTo(BoosterKind.Turbo));
        Assert.That(_runtime.IsRunning(BoosterKind.Turbo), Is.False);
        Assert.That(_runtime.GetState(BoosterKind.Turbo), Is.EqualTo(BoosterSlotState.Used));
    }

    [Test]
    public void ActivatingTheSameTypeTwice_IsRejected()
    {
        _runtime.SetEquipped(BoosterKind.ExtraTime, isEquipped: true);
        _runtime.TryActivate(BoosterKind.ExtraTime);

        var second = _runtime.TryActivate(BoosterKind.ExtraTime);

        Assert.That(second.TryGetFailure(out var failure), Is.True);
        Assert.That(failure.TryGetValue(out BoosterError.AlreadyUsed _), Is.True);
        Assert.That(_runtime.GetState(BoosterKind.ExtraTime), Is.EqualTo(BoosterSlotState.Used));
    }

    [Test]
    public void ExtraTime_IsInstantAndUsableWhileTurboRuns()
    {
        _runtime.SetEquipped(BoosterKind.Turbo, isEquipped: true);
        _runtime.SetEquipped(BoosterKind.ExtraTime, isEquipped: true);
        _runtime.TryActivate(BoosterKind.Turbo);

        Assert.That(_runtime.GetState(BoosterKind.ExtraTime), Is.EqualTo(BoosterSlotState.Ready));
        Assert.That(_runtime.TryActivate(BoosterKind.ExtraTime).IsSuccess, Is.True);
        Assert.That(_runtime.IsRunning(BoosterKind.ExtraTime), Is.False);
        Assert.That(_runtime.IsRunning(BoosterKind.Turbo), Is.True);
        Assert.That(_runtime.ExtraTimeSeconds, Is.EqualTo(15f));
    }

    [Test]
    public void Multipliers_AreAppliedOnlyWhileTurboRuns()
    {
        _runtime.SetEquipped(BoosterKind.Turbo, isEquipped: true);

        Assert.That(_runtime.SpeedMultiplier, Is.EqualTo(1f));
        Assert.That(_runtime.PowerMultiplier, Is.EqualTo(1f));

        _runtime.TryActivate(BoosterKind.Turbo);

        Assert.That(_runtime.SpeedMultiplier, Is.EqualTo(1.25f));
        Assert.That(_runtime.PowerMultiplier, Is.EqualTo(1.5f));

        _runtime.Tick(8f);

        Assert.That(_runtime.SpeedMultiplier, Is.EqualTo(1f));
        Assert.That(_runtime.PowerMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void ClearEffects_StopsRunningBoostersButKeepsThemUsed()
    {
        _runtime.SetEquipped(BoosterKind.Turbo, isEquipped: true);
        _runtime.TryActivate(BoosterKind.Turbo);

        Assert.That(_runtime.ClearEffects(), Is.EqualTo(1));
        Assert.That(_runtime.GetEnded(0), Is.EqualTo(BoosterKind.Turbo));
        Assert.That(_runtime.SpeedMultiplier, Is.EqualTo(1f));
        Assert.That(_runtime.GetState(BoosterKind.Turbo), Is.EqualTo(BoosterSlotState.Used));
        Assert.That(_runtime.ClearEffects(), Is.Zero);
    }

    [Test]
    public void Reset_ClearsEquippedUsedAndRunning()
    {
        _runtime.SetEquipped(BoosterKind.Turbo, isEquipped: true);
        _runtime.SetEquipped(BoosterKind.ExtraTime, isEquipped: true);
        _runtime.TryActivate(BoosterKind.Turbo);
        _runtime.TryActivate(BoosterKind.ExtraTime);

        _runtime.Reset();

        Assert.That(_runtime.IsEquipped(BoosterKind.Turbo), Is.False);
        Assert.That(_runtime.IsUsed(BoosterKind.Turbo), Is.False);
        Assert.That(_runtime.IsUsed(BoosterKind.ExtraTime), Is.False);
        Assert.That(_runtime.IsRunning(BoosterKind.Turbo), Is.False);
        Assert.That(_runtime.SpeedMultiplier, Is.EqualTo(1f));
    }

    [Test]
    public void BoosterKindLength_CoversEveryKindIndex()
    {
        Assert.That(BoosterKindExtensions.Length, Is.EqualTo((int)BoosterKind.ExtraTime + 1));
    }
}
