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
        , float SlowHintThreshold = CutMath.DEFAULT_SLOW_HINT
    )
    {
        public float Speed
        {
            get
            {
                var travel = new Vector2(To.x - From.x, To.z - From.z);

                return DeltaTime > 0f ? travel.magnitude / DeltaTime : 0f;
            }
        }
    }
}
