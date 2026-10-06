using EncosyTower.Common;
using GrassSimulation.Gameplay;
using GrassSimulation.Progression;

namespace GrassSimulation.UI
{
    public readonly record struct LastLevelResult(
          int Version
        , Option<LevelResult> Finished
        , Option<Result<LevelSettlement, SettleError>> Settlement
    );
}
