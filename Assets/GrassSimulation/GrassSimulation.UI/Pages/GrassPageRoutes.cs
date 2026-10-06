using GrassSimulation.Audio;
using GrassSimulation.Gameplay;

namespace GrassSimulation.UI
{
    public static class GrassPageRoutes
    {
        public static bool TryGetScreenKey(LevelState state, bool isHome, out string key)
        {
            if (isHome)
            {
                key = UiPageKeys.MAIN_MENU_SCREEN;
                return true;
            }

            switch (state)
            {
                case LevelState.Preview:
                {
                    key = UiPageKeys.PREVIEW_SCREEN;
                    return true;
                }

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

        public static bool TryGetPopupKey(LevelState state, bool isPaused, bool isHome, out string key)
        {
            if (isHome)
            {
                key = null;
                return false;
            }

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

        public static bool TryGetPopupSound(string fromKey, string toKey, out UiSound sound)
        {
            if (fromKey == null && IsSoundPopup(toKey))
            {
                sound = UiSound.PopupOpen;
                return true;
            }

            if (toKey == null && IsSoundPopup(fromKey))
            {
                sound = UiSound.PopupClose;
                return true;
            }

            sound = default;
            return false;
        }

        private static bool IsSoundPopup(string key)
        {
            return key == UiPageKeys.PAUSE_POPUP || key == UiPageKeys.UPGRADE_POPUP;
        }
    }
}
