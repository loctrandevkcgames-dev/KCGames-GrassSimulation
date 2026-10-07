using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class GameplayBoosterFormatTests
{
    [Test]
    public void GetName_NamesEachBooster()
    {
        Assert.That(GameplayBoosterFormat.GetName(BoosterKind.Turbo), Is.EqualTo("Turbo"));
        Assert.That(GameplayBoosterFormat.GetName(BoosterKind.ExtraTime), Is.EqualTo("Thêm giờ"));
    }

    [TestCase(3, "×3")]
    [TestCase(0, "×0")]
    [TestCase(-1, "×0")]
    public void FormatStock_ShowsTheCountAndNeverNegative(int stock, string expected)
    {
        Assert.That(GameplayBoosterFormat.FormatStock(stock), Is.EqualTo(expected));
    }

    [TestCase(15f, "+15s")]
    [TestCase(14.6f, "+15s")]
    public void FormatExtraTime_ShowsThePlusSeconds(float seconds, string expected)
    {
        Assert.That(GameplayBoosterFormat.FormatExtraTime(seconds), Is.EqualTo(expected));
    }

    [TestCase(8f, "8s")]
    [TestCase(7.01f, "8s")]
    [TestCase(0.2f, "1s")]
    [TestCase(0f, "0s")]
    public void FormatRunning_RoundsTheRemainingSecondsUp(float remaining, string expected)
    {
        Assert.That(GameplayBoosterFormat.FormatRunning(remaining), Is.EqualTo(expected));
    }

    [Test]
    public void EachSlotStateHasItsOwnGlyphSoColorIsNeverTheOnlySignal()
    {
        var ready = Slot(BoosterSlotState.Ready);
        var running = Slot(BoosterSlotState.Running, remaining: 5f);
        var blocked = Slot(BoosterSlotState.Blocked);
        var used = Slot(BoosterSlotState.Used);

        var glyphs = new[] {
            GameplayBoosterFormat.GetGlyph(in ready),
            GameplayBoosterFormat.GetGlyph(in running),
            GameplayBoosterFormat.GetGlyph(in blocked),
            GameplayBoosterFormat.GetGlyph(in used),
        };

        Assert.That(glyphs, Is.Unique);
        Assert.That(glyphs[1], Is.EqualTo("5s"));
    }

    [Test]
    public void OnlyAReadyBoosterIsTappableAndOnlyAnEquippedOneIsShown()
    {
        Assert.That(GameplayBoosterFormat.IsTappable(BoosterSlotState.Ready), Is.True);
        Assert.That(GameplayBoosterFormat.IsTappable(BoosterSlotState.Running), Is.False);
        Assert.That(GameplayBoosterFormat.IsTappable(BoosterSlotState.Blocked), Is.False);
        Assert.That(GameplayBoosterFormat.IsTappable(BoosterSlotState.Used), Is.False);
        Assert.That(GameplayBoosterFormat.IsTappable(BoosterSlotState.NotEquipped), Is.False);

        Assert.That(GameplayBoosterFormat.IsShown(BoosterSlotState.NotEquipped), Is.False);
        Assert.That(GameplayBoosterFormat.IsShown(BoosterSlotState.Used), Is.True);
    }

    [Test]
    public void GetRingFill_ShrinksWithTheRemainingTimeOnlyWhileRunning()
    {
        var full = Slot(BoosterSlotState.Running, remaining: 8f);
        var half = Slot(BoosterSlotState.Running, remaining: 4f);
        var over = Slot(BoosterSlotState.Running, remaining: 20f);
        var ready = Slot(BoosterSlotState.Ready, remaining: 8f);

        Assert.That(GameplayBoosterFormat.GetRingFill(in full), Is.EqualTo(1f));
        Assert.That(GameplayBoosterFormat.GetRingFill(in half), Is.EqualTo(0.5f));
        Assert.That(GameplayBoosterFormat.GetRingFill(in over), Is.EqualTo(1f));
        Assert.That(GameplayBoosterFormat.GetRingFill(in ready), Is.Zero);
    }

    [Test]
    public void Colors_DifferPerState()
    {
        Assert.That(GameplayBoosterFormat.GetBackground(BoosterSlotState.Ready), Is.Not.EqualTo(UiPalette.Primary));
        Assert.That(GameplayBoosterFormat.GetBackground(BoosterSlotState.Running), Is.EqualTo(UiPalette.Primary));
        Assert.That(GameplayBoosterFormat.GetInk(BoosterSlotState.Running), Is.EqualTo(UiPalette.White));
        Assert.That(GameplayBoosterFormat.GetInk(BoosterSlotState.Ready), Is.EqualTo(UiPalette.Ink));
    }

    private static BoosterSlot Slot(BoosterSlotState state, float remaining = 0f)
        => new(state, IsEquipped: true, IsAllowed: true, Stock: 3, Remaining: remaining, Duration: 8f);
}
