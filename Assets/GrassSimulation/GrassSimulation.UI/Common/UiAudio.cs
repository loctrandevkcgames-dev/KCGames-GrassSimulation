using EncosyTower.PubSub;
using GrassSimulation.Audio;

namespace GrassSimulation.UI
{
    public static class UiAudio
    {
        public static void Request(UiSound sound, int count = 1)
        {
            var publisher = GlobalMessenger.Publisher.Scope<AudioScope>();

            UiSoundRequestedMsg.Publish(in publisher, new UiSoundRequestedMsg(sound, count));
        }

        public static void Tap()
        {
            Request(UiSound.Tap);
        }
    }
}
