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

        public static bool TryGetPopupKey(LevelState state, out string key)
        {
            if (state == LevelState.UpgradeChoice)
            {
                key = UiPageKeys.UPGRADE_POPUP;
                return true;
            }

            key = null;
            return false;
        }
    }
}
