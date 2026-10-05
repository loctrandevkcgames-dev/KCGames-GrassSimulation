using EncosyTower.CodeGen;
using EncosyTower.Common;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(ProgressionScope))]
    public readonly partial record struct LevelSettledMsg(LevelId Level, Result<LevelSettlement, SettleError> Outcome);
}
