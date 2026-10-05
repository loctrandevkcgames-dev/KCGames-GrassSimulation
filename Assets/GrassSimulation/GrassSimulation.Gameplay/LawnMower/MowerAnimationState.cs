using EncosyTower.EnumExtensions;

namespace GrassSimulation.Gameplay
{
    [EnumExtensions]
    public enum MowerAnimationState : byte
    {
        Parked,
        Running,
        Celebrating,
        Stalled,
    }
}
