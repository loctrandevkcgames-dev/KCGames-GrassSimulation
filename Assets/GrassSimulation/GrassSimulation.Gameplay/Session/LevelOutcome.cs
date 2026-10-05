using EncosyTower.EnumExtensions;

namespace GrassSimulation.Gameplay
{
    [EnumExtensions]
    public enum LevelOutcome : byte
    {
        None,
        Success,
        TimeUp,
        TooManyProtectedHits,
    }
}
