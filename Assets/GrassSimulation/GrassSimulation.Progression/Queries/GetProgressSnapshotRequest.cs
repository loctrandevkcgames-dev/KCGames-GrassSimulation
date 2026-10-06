using EncosyTower.CodeGen;
using EncosyTower.Processing;

namespace GrassSimulation.Progression
{
    [Processing(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(ProgressionScope))]
    public readonly partial record struct GetProgressSnapshotRequest : IRequest<ProgressSnapshot>;
}
