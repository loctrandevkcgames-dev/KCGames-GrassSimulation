using EncosyTower.EnumExtensions;

namespace GrassSimulation.Gameplay
{
    [EnumExtensions]
    public enum LevelState : byte
    {
        Preview,
        Playing,
        UpgradeChoice,
        Success,
        Failure,
        Cleanup,
    }
}
