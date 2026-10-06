using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class GrassPageRoutesTests
{
    [TestCase(LevelState.Playing)]
    [TestCase(LevelState.Cleanup)]
    public void TryGetScreenKey_MapsPlayingStatesToGameplayScreen(LevelState state)
    {
        Assert.That(GrassPageRoutes.TryGetScreenKey(state, out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.GAMEPLAY_SCREEN));
    }

    [Test]
    public void TryGetScreenKey_ReturnsFalseForStatesWithoutScreen()
    {
        foreach (LevelState state in System.Enum.GetValues(typeof(LevelState)))
        {
            if (state == LevelState.Playing || state == LevelState.Cleanup)
            {
                continue;
            }

            Assert.That(GrassPageRoutes.TryGetScreenKey(state, out var key), Is.False, state.ToString());
            Assert.That(key, Is.Null);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TryGetPopupKey_MapsUpgradeChoiceToUpgradePopup(bool isPaused)
    {
        Assert.That(GrassPageRoutes.TryGetPopupKey(LevelState.UpgradeChoice, isPaused, out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.UPGRADE_POPUP));
        Assert.That(key, Is.EqualTo("ui/upgrade-popup"));
    }

    [TestCase(LevelState.Success, false)]
    [TestCase(LevelState.Success, true)]
    [TestCase(LevelState.Failure, false)]
    [TestCase(LevelState.Failure, true)]
    public void TryGetPopupKey_MapsFinishedStatesToResultPopup(LevelState state, bool isPaused)
    {
        Assert.That(GrassPageRoutes.TryGetPopupKey(state, isPaused, out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.RESULT_POPUP));
        Assert.That(key, Is.EqualTo("ui/result-popup"));
    }

    [TestCase(LevelState.Playing)]
    [TestCase(LevelState.Cleanup)]
    public void TryGetPopupKey_MapsPausedPlayingStatesToPausePopup(LevelState state)
    {
        Assert.That(GrassPageRoutes.TryGetPopupKey(state, isPaused: true, out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.PAUSE_POPUP));
        Assert.That(key, Is.EqualTo("ui/pause-popup"));
    }

    [TestCase(LevelState.Playing)]
    [TestCase(LevelState.Cleanup)]
    public void TryGetPopupKey_ReturnsFalseForUnpausedPlayingStates(LevelState state)
    {
        Assert.That(GrassPageRoutes.TryGetPopupKey(state, isPaused: false, out var key), Is.False);
        Assert.That(key, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TryGetPopupKey_ReturnsFalseForPreview(bool isPaused)
    {
        Assert.That(GrassPageRoutes.TryGetPopupKey(LevelState.Preview, isPaused, out var key), Is.False);
        Assert.That(key, Is.Null);
    }

    [Test]
    public void UpgradeChoice_KeepsTheGameplayScreenRouteUnchanged()
    {
        Assert.That(GrassPageRoutes.TryGetScreenKey(LevelState.UpgradeChoice, out _), Is.False);
    }
}
