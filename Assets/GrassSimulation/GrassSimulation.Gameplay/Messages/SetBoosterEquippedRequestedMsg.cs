using EncosyTower.CodeGen;
using EncosyTower.PubSub;

namespace GrassSimulation.Gameplay
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(LevelCommandScope))]
    public readonly partial record struct SetBoosterEquippedRequestedMsg(BoosterKind Kind, bool IsEquipped);
}
