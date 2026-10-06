using EncosyTower.CodeGen;
using EncosyTower.PubSub;

namespace GrassSimulation.Gameplay
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(GameplayScope))]
    public readonly partial record struct LockedPlantTouchedMsg(PlantKind Kind, int RequiredTier);
}
