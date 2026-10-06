using GrassSimulation.Audio;
using GrassSimulation.Gameplay;
using NUnit.Framework;

namespace GrassSimulation.UI.Tests;

public sealed class GrassPageRoutesTests
{
    [TestCase(LevelState.Playing)]
    [TestCase(LevelState.Cleanup)]
    public void TryGetScreenKey_MapsPlayingStatesToGameplayScreen(LevelState state)
    {
        Assert.That(TryScreen(state: state, isHome: false, key: out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.GAMEPLAY_SCREEN));
    }

    [Test]
    public void TryGetScreenKey_MapsPreviewToPreviewScreen()
    {
        Assert.That(TryScreen(state: LevelState.Preview, isHome: false, key: out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.PREVIEW_SCREEN));
        Assert.That(key, Is.EqualTo("ui/preview-screen"));
    }

    [Test]
    public void TryGetScreenKey_MapsEveryStateToMainMenuScreenWhileHome()
    {
        foreach (LevelState state in System.Enum.GetValues(typeof(LevelState)))
        {
            Assert.That(TryScreen(state: state, isHome: true, key: out var key), Is.True, state.ToString());
            Assert.That(key, Is.EqualTo(UiPageKeys.MAIN_MENU_SCREEN), state.ToString());
        }

        Assert.That(UiPageKeys.MAIN_MENU_SCREEN, Is.EqualTo("ui/main-menu-screen"));
    }

    [Test]
    public void TryGetScreenKey_ReturnsFalseForStatesWithoutScreen()
    {
        foreach (LevelState state in System.Enum.GetValues(typeof(LevelState)))
        {
            var hasScreen = state == LevelState.Preview || state == LevelState.Playing || state == LevelState.Cleanup;

            if (hasScreen)
            {
                continue;
            }

            Assert.That(TryScreen(state: state, isHome: false, key: out var key), Is.False, state.ToString());
            Assert.That(key, Is.Null);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TryGetPopupKey_MapsUpgradeChoiceToUpgradePopup(bool isPaused)
    {
        var hasKey = TryPopup(state: LevelState.UpgradeChoice, isPaused: isPaused, isHome: false, key: out var key);

        Assert.That(hasKey, Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.UPGRADE_POPUP));
        Assert.That(key, Is.EqualTo("ui/upgrade-popup"));
    }

    [TestCase(LevelState.Success, false)]
    [TestCase(LevelState.Success, true)]
    [TestCase(LevelState.Failure, false)]
    [TestCase(LevelState.Failure, true)]
    public void TryGetPopupKey_MapsFinishedStatesToResultPopup(LevelState state, bool isPaused)
    {
        Assert.That(TryPopup(state: state, isPaused: isPaused, isHome: false, key: out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.RESULT_POPUP));
        Assert.That(key, Is.EqualTo("ui/result-popup"));
    }

    [TestCase(LevelState.Playing)]
    [TestCase(LevelState.Cleanup)]
    public void TryGetPopupKey_MapsPausedPlayingStatesToPausePopup(LevelState state)
    {
        Assert.That(TryPopup(state: state, isPaused: true, isHome: false, key: out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.PAUSE_POPUP));
        Assert.That(key, Is.EqualTo("ui/pause-popup"));
    }

    [TestCase(LevelState.Playing)]
    [TestCase(LevelState.Cleanup)]
    public void TryGetPopupKey_ReturnsFalseForUnpausedPlayingStates(LevelState state)
    {
        Assert.That(TryPopup(state: state, isPaused: false, isHome: false, key: out var key), Is.False);
        Assert.That(key, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TryGetPopupKey_ReturnsFalseForPreview(bool isPaused)
    {
        Assert.That(TryPopup(state: LevelState.Preview, isPaused: isPaused, isHome: false, key: out var key), Is.False);
        Assert.That(key, Is.Null);
    }

    [Test]
    public void TryGetPopupKey_ReturnsFalseForEveryStateWhileHome()
    {
        foreach (LevelState state in System.Enum.GetValues(typeof(LevelState)))
        {
            var hasKey = TryPopup(state: state, isPaused: true, isHome: true, key: out var key);

            Assert.That(hasKey, Is.False, state.ToString());
            Assert.That(key, Is.Null, state.ToString());
        }
    }

    [Test]
    public void UpgradeChoice_KeepsTheGameplayScreenRouteUnchanged()
    {
        Assert.That(TryScreen(state: LevelState.UpgradeChoice, isHome: false, key: out _), Is.False);
    }

    [TestCase(UiPageKeys.PAUSE_POPUP)]
    [TestCase(UiPageKeys.UPGRADE_POPUP)]
    public void TryGetPopupSound_OpensWhenAPopupAppears(string toKey)
    {
        Assert.That(GrassPageRoutes.TryGetPopupSound(fromKey: null, toKey: toKey, sound: out var sound), Is.True);
        Assert.That(sound, Is.EqualTo(UiSound.PopupOpen));
    }

    [TestCase(UiPageKeys.PAUSE_POPUP)]
    [TestCase(UiPageKeys.UPGRADE_POPUP)]
    public void TryGetPopupSound_ClosesWhenAPopupDisappears(string fromKey)
    {
        Assert.That(GrassPageRoutes.TryGetPopupSound(fromKey: fromKey, toKey: null, sound: out var sound), Is.True);
        Assert.That(sound, Is.EqualTo(UiSound.PopupClose));
    }

    [Test]
    public void TryGetPopupSound_StaysSilentForTheResultPopup()
    {
        Assert.That(GrassPageRoutes.TryGetPopupSound(fromKey: null, toKey: UiPageKeys.RESULT_POPUP, sound: out _), Is.False);
        Assert.That(GrassPageRoutes.TryGetPopupSound(fromKey: UiPageKeys.RESULT_POPUP, toKey: null, sound: out _), Is.False);
        Assert.That(
              GrassPageRoutes.TryGetPopupSound(fromKey: UiPageKeys.UPGRADE_POPUP, toKey: UiPageKeys.RESULT_POPUP, sound: out _)
            , Is.False
        );
    }

    [Test]
    public void TryGetPopupSound_StaysSilentWithoutAChange()
    {
        Assert.That(GrassPageRoutes.TryGetPopupSound(fromKey: null, toKey: null, sound: out _), Is.False);
    }

    private static bool TryScreen(LevelState state, bool isHome, out string key)
    {
        return GrassPageRoutes.TryGetScreenKey(state: state, isHome: isHome, key: out key);
    }

    private static bool TryPopup(LevelState state, bool isPaused, bool isHome, out string key)
    {
        return GrassPageRoutes.TryGetPopupKey(state: state, isPaused: isPaused, isHome: isHome, key: out key);
    }
}
