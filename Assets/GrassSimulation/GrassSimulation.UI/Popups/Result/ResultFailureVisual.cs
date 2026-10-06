using UnityEngine;

namespace GrassSimulation.UI
{
    public readonly record struct ResultFailureVisual(
          string Title
        , string Reason
        , ResultFailureIcon Icon
        , Color Tile
        , Color IconTint
    );
}
