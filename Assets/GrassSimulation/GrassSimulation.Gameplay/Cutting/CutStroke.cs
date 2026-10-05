using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public readonly record struct CutStroke(
          Vector3 From
        , Vector3 To
        , float Radius
        , int Tier
        , float CuttingPower
        , float DeltaTime
    );
}
