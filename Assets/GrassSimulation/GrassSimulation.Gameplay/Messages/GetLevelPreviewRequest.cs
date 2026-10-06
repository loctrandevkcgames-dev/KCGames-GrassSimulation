using EncosyTower.CodeGen;
using EncosyTower.Processing;

namespace GrassSimulation.Gameplay
{
    [Processing(ApiMode.Sync, State = StateMode.Stateless, Scope = typeof(GameplayScope))]
    public readonly partial record struct GetLevelPreviewRequest(int LevelIndex) : IRequest<LevelPreview>;
}
