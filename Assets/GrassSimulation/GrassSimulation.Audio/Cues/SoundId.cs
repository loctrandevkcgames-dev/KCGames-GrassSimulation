using EncosyTower.EnumExtensions;

namespace GrassSimulation.Audio
{
    [EnumExtensions]
    public enum SoundId : byte
    {
        BushTrim,
        FruitPop,
        QuotaComplete,
        TierUp,
        UpgradeChosen,
        ProtectedHit,
        LevelStart,
        TimerTick,
        TimerTickAccent,
        Coin,
        Star,
        Tap,
        PopupOpen,
        PopupClose,
        ToggleOn,
        ToggleOff,
        JingleWin,
        JingleLose,
    }
}
