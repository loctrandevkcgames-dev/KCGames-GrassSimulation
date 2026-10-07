using EncosyTower.CodeGen;
using EncosyTower.PubSub;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    [PubSub(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(ProgressionScope))]
    public readonly partial record struct UnlockGrantedMsg(LevelId Level, UnlockSettings Unlock);
}
