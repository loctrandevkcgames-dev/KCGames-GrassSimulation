using EncosyTower.CodeGen;
using EncosyTower.Processing;

namespace GrassSimulation.UI
{
    [Processing(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(UiScope))]
    public readonly partial record struct GetLastLevelResultRequest : IRequest<LastLevelResult>;
}
