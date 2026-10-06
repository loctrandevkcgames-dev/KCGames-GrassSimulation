using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class GrassPageRoutes
    {
        public static bool TryGetScreenKey(LevelState state, out string key)
        {
            switch (state)
            {
                case LevelState.Playing:
                case LevelState.Cleanup:
                {
                    key = UiPageKeys.GAMEPLAY_SCREEN;
                    return true;
                }

                default:
                {
                    key = null;
                    return false;
                }
            }
        }

        public static bool TryGetPopupKey(LevelState state, bool isPaused, out string key)
        {
            switch (state)
            {
                case LevelState.UpgradeChoice:
                {
                    key = UiPageKeys.UPGRADE_POPUP;
                    return true;
                }

                case LevelState.Success:
                case LevelState.Failure:
                {
                    key = UiPageKeys.RESULT_POPUP;
                    return true;
                }

                case LevelState.Playing:
                case LevelState.Cleanup:
                {
                    key = isPaused ? UiPageKeys.PAUSE_POPUP : null;
                    return isPaused;
                }

                default:
                {
                    key = null;
                    return false;
                }
            }
        }
    }
}
