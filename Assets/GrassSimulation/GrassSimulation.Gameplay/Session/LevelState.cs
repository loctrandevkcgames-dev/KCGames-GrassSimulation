using EncosyTower.EnumExtensions;

namespace GrassSimulation.Gameplay
{
    [EnumExtensions]
    public enum LevelState : byte
    {
        Preview,
        Loadout,
        Playing,
        UpgradeChoice,
        Success,
        Failure,
        Cleanup,
    }
}
