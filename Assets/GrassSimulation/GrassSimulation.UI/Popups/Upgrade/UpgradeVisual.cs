using UnityEngine;

namespace GrassSimulation.UI
{
    public readonly record struct UpgradeVisual(
          string Title
        , bool HasHint
        , string Hint
        , UpgradeIcon Icon
        , Color Tile
        , Color IconTint
        , float IconSize
        , UpgradeStatKinds Stats
        , bool ShowUnchangedStats
    );
}
