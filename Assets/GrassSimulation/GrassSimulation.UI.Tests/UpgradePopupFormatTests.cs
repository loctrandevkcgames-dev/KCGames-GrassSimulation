using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class UpgradePopupFormatTests
{
    private static readonly MachineStats s_base = new(CutRadius: 0.65f, CuttingPower: 1f, Speed: 4f);

    [Test]
    public void FormatLevelUp_ShowsTheTier()
    {
        Assert.That(UpgradePopupFormat.FormatLevelUp(tier: 3), Is.EqualTo("Lên cấp 3!"));
    }

    [Test]
    public void FormatUnits_UseTwoDecimalsAndInvariantCulture()
    {
        Assert.That(UpgradePopupFormat.FormatMeters(0.8f), Is.EqualTo("0.80 m"));
        Assert.That(UpgradePopupFormat.FormatMeters(0.95f), Is.EqualTo("0.95 m"));
        Assert.That(UpgradePopupFormat.FormatSpeed(4.5f), Is.EqualTo("4.50 m/s"));
        Assert.That(UpgradePopupFormat.FormatNumber(1.3f), Is.EqualTo("1.30"));
    }

    [Test]
    public void TryFormatUnlocked_FailsWhenNothingUnlocks()
    {
        Assert.That(UpgradePopupFormat.TryFormatUnlocked(kinds: default, out var text), Is.False);
        Assert.That(text, Is.Null);
    }

    [Test]
    public void TryFormatUnlocked_NamesOneKind()
    {
        var mask = default(PlantKindMask).With(PlantKind.HardBush);

        Assert.That(UpgradePopupFormat.TryFormatUnlocked(mask, out var text), Is.True);
        Assert.That(text, Is.EqualTo("Đã mở khóa bụi cứng"));
    }

    [Test]
    public void TryFormatUnlocked_JoinsTwoKindsWithAnd()
    {
        var mask = default(PlantKindMask).With(PlantKind.HardBush).With(PlantKind.LowBush);

        Assert.That(UpgradePopupFormat.TryFormatUnlocked(mask, out var text), Is.True);
        Assert.That(text, Is.EqualTo("Đã mở khóa bụi rậm và bụi cứng"));
    }

    [Test]
    public void TryFormatUnlocked_JoinsThreeKindsWithCommaAndAnd()
    {
        var mask = default(PlantKindMask)
            .With(PlantKind.HarvestFlower)
            .With(PlantKind.LowBush)
            .With(PlantKind.HardBush);

        Assert.That(UpgradePopupFormat.TryFormatUnlocked(mask, out var text), Is.True);
        Assert.That(text, Is.EqualTo("Đã mở khóa hoa đỏ, bụi rậm và bụi cứng"));
    }

    [Test]
    public void GetStatKinds_WideBladeShowsOnlyRadius()
    {
        var visual = UpgradeVisuals.Get(UpgradeVisuals.WIDE_BLADE);
        var after = s_base with { CutRadius = 0.8f };

        Assert.That(
              UpgradePopupFormat.GetStatKinds(in visual, in s_base, in after)
            , Is.EqualTo(UpgradeStatKinds.CutRadius)
        );
    }

    [Test]
    public void GetStatKinds_StrongEngineShowsPowerAndSpeed()
    {
        var visual = UpgradeVisuals.Get(UpgradeVisuals.STRONG_ENGINE);
        var after = s_base with { CuttingPower = 1.3f, Speed = 4.25f };

        Assert.That(
              UpgradePopupFormat.GetStatKinds(in visual, in s_base, in after)
            , Is.EqualTo(UpgradeStatKinds.CuttingPower | UpgradeStatKinds.Speed)
        );
    }

    [Test]
    public void GetStatKinds_KnownUpgradeKeepsItsStatsEvenWhenUnchanged()
    {
        var visual = UpgradeVisuals.Get(UpgradeVisuals.WIDE_BLADE);

        Assert.That(
              UpgradePopupFormat.GetStatKinds(in visual, in s_base, in s_base)
            , Is.EqualTo(UpgradeStatKinds.CutRadius)
        );
    }

    [Test]
    public void GetStatKinds_UnknownIdShowsOnlyChangedStats()
    {
        var visual = UpgradeVisuals.Get("Unknown");
        var after = s_base with { Speed = 4.5f, CutRadius = 0.65f + 1e-6f };

        Assert.That(
              UpgradePopupFormat.GetStatKinds(in visual, in s_base, in after)
            , Is.EqualTo(UpgradeStatKinds.Speed)
        );
        Assert.That(
              UpgradePopupFormat.GetStatKinds(in visual, in s_base, in s_base)
            , Is.EqualTo(UpgradeStatKinds.None)
        );
    }

    [Test]
    public void Get_UnknownIdFallsBackToGenericVisualUsingTheId()
    {
        var visual = UpgradeVisuals.Get("Unknown");

        Assert.That(visual.Title, Is.EqualTo("Unknown"));
        Assert.That(visual.HasHint, Is.False);
        Assert.That(visual.ShowUnchangedStats, Is.False);
    }

    [Test]
    public void Get_KnownIdsCarryDesignTitlesAndHints()
    {
        var wideBlade = UpgradeVisuals.Get(UpgradeVisuals.WIDE_BLADE);
        var strongEngine = UpgradeVisuals.Get(UpgradeVisuals.STRONG_ENGINE);

        Assert.That(wideBlade.Title, Is.EqualTo("Lưỡi rộng"));
        Assert.That(wideBlade.Icon, Is.EqualTo(UpgradeIcon.Rings));
        Assert.That(wideBlade.HasHint, Is.True);
        Assert.That(strongEngine.Title, Is.EqualTo("Động cơ khỏe"));
        Assert.That(strongEngine.Icon, Is.EqualTo(UpgradeIcon.Bolt));
        Assert.That(strongEngine.HasHint, Is.True);
    }

    [Test]
    public void FormatStatLine_ColorsTheAfterValue()
    {
        var after = s_base with { CutRadius = 0.95f };

        Assert.That(
              UpgradePopupFormat.FormatStatLine(UpgradeStatKinds.CutRadius, in s_base, in after)
            , Is.EqualTo("Bán kính cắt 0.65 m <color=#2E7D32>→ 0.95 m</color>")
        );
    }

    [Test]
    public void FormatStatLine_PutsTheSpeedUnitOnTheAfterValueOnly()
    {
        var before = s_base with { Speed = 4.25f };
        var after = s_base with { Speed = 4.5f };

        Assert.That(
              UpgradePopupFormat.FormatStatLine(UpgradeStatKinds.Speed, in before, in after)
            , Is.EqualTo("Tốc độ 4.25 <color=#2E7D32>→ 4.50 m/s</color>")
        );
    }

    [Test]
    public void FormatStatLine_CuttingPowerHasNoUnit()
    {
        var before = s_base with { CuttingPower = 1.3f };
        var after = s_base with { CuttingPower = 1.6f };

        Assert.That(
              UpgradePopupFormat.FormatStatLine(UpgradeStatKinds.CuttingPower, in before, in after)
            , Is.EqualTo("Sức cắt 1.30 <color=#2E7D32>→ 1.60</color>")
        );
    }

    [Test]
    public void FormatStatLine_AlwaysShowsBeforeAndAfterWithoutAMaxSuffix()
    {
        var wide = s_base with { CutRadius = 1.1f };
        var wider = s_base with { CutRadius = 1.25f };

        Assert.That(
              UpgradePopupFormat.FormatStatLine(UpgradeStatKinds.CutRadius, in wide, in wider)
            , Is.EqualTo("Bán kính cắt 1.10 m <color=#2E7D32>→ 1.25 m</color>")
        );
        Assert.That(
              UpgradePopupFormat.FormatStatLine(UpgradeStatKinds.CutRadius, in wide, in wide)
            , Does.Not.Contain("tối đa")
        );
    }

    [Test]
    public void InputLock_BlocksTapsForZeroPointFourSeconds()
    {
        var inputLock = default(UpgradeInputLock);

        inputLock.Start(now: 10f);

        Assert.That(UpgradeInputLock.DURATION, Is.EqualTo(0.4f));
        Assert.That(inputLock.IsLocked(now: 10f), Is.True);
        Assert.That(inputLock.IsLocked(now: 10.39f), Is.True);
        Assert.That(inputLock.IsLocked(now: 10.4f), Is.False);
        Assert.That(default(UpgradeInputLock).IsLocked(now: 0f), Is.False);
    }
}
