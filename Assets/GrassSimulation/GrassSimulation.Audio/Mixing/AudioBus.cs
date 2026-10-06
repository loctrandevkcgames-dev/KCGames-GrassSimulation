using EncosyTower.EnumExtensions;

namespace GrassSimulation.Audio
{
    [EnumExtensions]
    public enum AudioBus : byte
    {
        Music,
        Ambience,
        SfxLoops,
        SfxEvents,
        Ui,
    }
}
