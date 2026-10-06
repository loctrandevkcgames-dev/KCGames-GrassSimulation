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
}
