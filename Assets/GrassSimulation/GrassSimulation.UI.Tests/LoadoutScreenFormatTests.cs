using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class LoadoutScreenFormatTests
{
    private static readonly LoadoutMachine s_standard = new(
          MachineIds.Standard
        , "Standard"
        , "Cân bằng"
        , new MachineStats(CutRadius: 0.65f, CuttingPower: 1f, Speed: 4f)
    );

    private static readonly LoadoutMachine s_wide = new(
          MachineIds.Wide
        , "Wide"
        , "Phủ rộng"
        , new MachineStats(CutRadius: 0.78f, CuttingPower: 1f, Speed: 3.6f)
    );

    [Test]
    public void FormatEquip_DistinguishesTheStateWithWordsNotColor()
    {
        Assert.That(LoadoutScreenFormat.FormatEquip(isEquipped: false), Is.EqualTo("Mang theo"));
        Assert.That(LoadoutScreenFormat.FormatEquip(isEquipped: true), Is.EqualTo("Đang mang"));
    }

    [Test]
    public void IsBoosterListed_NeedsStockAndAnAllowedLevel()
    {
        var listed = new BoosterSlot(BoosterSlotState.NotEquipped, false, IsAllowed: true, Stock: 3, 0f, 8f);
        var empty = listed with { Stock = 0 };
        var blocked = listed with { IsAllowed = false };

        Assert.That(LoadoutScreenFormat.IsBoosterListed(in listed), Is.True);
        Assert.That(LoadoutScreenFormat.IsBoosterListed(in empty), Is.False);
        Assert.That(LoadoutScreenFormat.IsBoosterListed(in blocked), Is.False);
    }

    [TestCase(0.65f, 0.78f, 0.8333f)]
    [TestCase(0.78f, 0.78f, 1f)]
    [TestCase(3.6f, 4f, 0.9f)]
    [TestCase(1f, 0f, 0f)]
    [TestCase(5f, 4f, 1f)]
    public void GetBarFill_IsRelativeToTheBestAndClamped(float value, float best, float expected)
    {
        Assert.That(LoadoutScreenFormat.GetBarFill(value, best), Is.EqualTo(expected).Within(1e-3f));
    }

    [Test]
    public void GetBestStats_TakesTheBestOfEachStatAcrossOwnedMachines()
    {
        var snapshot = new LoadoutSnapshot(2, MachineIds.Standard, s_standard, s_wide, default);
        var best = LoadoutScreenFormat.GetBestStats(in snapshot);

        Assert.That(best.CutRadius, Is.EqualTo(0.78f));
        Assert.That(best.Speed, Is.EqualTo(4f));
        Assert.That(best.CuttingPower, Is.EqualTo(1f));
    }

    [Test]
    public void GetBestStats_IgnoresSlotsBeyondTheMachineCount()
    {
        var snapshot = new LoadoutSnapshot(1, MachineIds.Standard, s_standard, s_wide, default);
        var best = LoadoutScreenFormat.GetBestStats(in snapshot);

        Assert.That(best.CutRadius, Is.EqualTo(0.65f));
    }

    [TestCase(UnlockKind.WideMachine, 0, "Mở khóa: Máy Wide")]
    [TestCase(UnlockKind.ExtraTimeBooster, 3, "Mở khóa: Thêm giờ ×3")]
    [TestCase(UnlockKind.TurboBooster, 3, "Mở khóa: Turbo ×3")]
    public void FormatUnlockCard_NamesTheUnlock(UnlockKind kind, int amount, string expected)
    {
        var unlock = new UnlockSettings { Kind = kind, Amount = amount };

        Assert.That(LoadoutScreenFormat.FormatUnlockCard(unlock), Is.EqualTo(expected));
    }

    [Test]
    public void TryFormatUnlocks_ReturnsFalseWithoutUnlocks()
    {
        Assert.That(LoadoutScreenFormat.TryFormatUnlocks(null, out var text), Is.False);
        Assert.That(text, Is.Null);
        Assert.That(LoadoutScreenFormat.TryFormatUnlocks(new UnlockSettings[0], out _), Is.False);
    }

    [Test]
    public void TryFormatUnlocks_JoinsEveryUnlockOnItsOwnLine()
    {
        var unlocks = new[] {
            new UnlockSettings { Kind = UnlockKind.WideMachine },
            new UnlockSettings { Kind = UnlockKind.TurboBooster, Amount = 3 },
        };

        Assert.That(LoadoutScreenFormat.TryFormatUnlocks(unlocks, out var text), Is.True);
        Assert.That(text, Does.StartWith("Mở khóa: Máy Wide"));
        Assert.That(text, Does.EndWith("Mở khóa: Turbo ×3"));
    }
}
