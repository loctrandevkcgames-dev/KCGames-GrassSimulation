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

    [Test]
    public void TryGetPopupKey_MapsUpgradeChoiceToUpgradePopup()
    {
        Assert.That(GrassPageRoutes.TryGetPopupKey(LevelState.UpgradeChoice, out var key), Is.True);
        Assert.That(key, Is.EqualTo(UiPageKeys.UPGRADE_POPUP));
        Assert.That(key, Is.EqualTo("ui/upgrade-popup"));
    }

    [Test]
    public void TryGetPopupKey_ReturnsFalseForOtherStates()
    {
        foreach (LevelState state in System.Enum.GetValues(typeof(LevelState)))
        {
            if (state == LevelState.UpgradeChoice)
            {
                continue;
            }

            Assert.That(GrassPageRoutes.TryGetPopupKey(state, out var key), Is.False, state.ToString());
            Assert.That(key, Is.Null);
        }
    }

    [Test]
    public void UpgradeChoice_KeepsTheGameplayScreenRouteUnchanged()
    {
        Assert.That(GrassPageRoutes.TryGetScreenKey(LevelState.UpgradeChoice, out _), Is.False);
    }
}
