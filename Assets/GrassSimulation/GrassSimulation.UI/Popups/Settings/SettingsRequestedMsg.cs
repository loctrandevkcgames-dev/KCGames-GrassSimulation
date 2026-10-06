using EncosyTower.CodeGen;
using EncosyTower.PubSub;

namespace GrassSimulation.UI
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(UiScope))]
    public readonly partial record struct SettingsRequestedMsg(bool IsOpen);
}
