using EncosyTower.CodeGen;
using EncosyTower.PubSub;

namespace GrassSimulation.Gameplay
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(GameplayScope))]
    public readonly partial record struct HarvestBatchedMsg(int Cells, int Xp);
}
