using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public readonly record struct JoystickState(
          bool IsDragging
        , Vector2 Origin
        , Vector2 Input
        , float Radius
        , bool IsMoving
    );
}
