using EncosyTower.CodeGen;
using EncosyTower.PubSub;

namespace GrassSimulation.Audio
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(AudioScope))]
    public readonly partial record struct UiSoundRequestedMsg(UiSound Sound, int Count);
}
