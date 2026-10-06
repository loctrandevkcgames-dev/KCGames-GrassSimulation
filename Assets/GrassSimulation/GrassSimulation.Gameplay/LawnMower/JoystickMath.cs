using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public static class JoystickMath
    {
        public const float DEAD_ZONE = 0.1f;
        public const float DESIGN_WIDTH = 390f;
        public const float DESIGN_RADIUS = 66f;
        public const float RADIUS_WIDTH_FRACTION = DESIGN_RADIUS / DESIGN_WIDTH;
        public const float MIN_RADIUS = 60f;

        public static float GetRadius(float referenceSize, float sizeFraction, float minRadius)
        {
            return Mathf.Max(referenceSize * sizeFraction, minRadius);
        }

        public static bool IsInRegion(Vector2 position, Vector2 screenSize, Rect region)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return false;
            }

            var x = position.x / screenSize.x;
            var y = position.y / screenSize.y;

            return x >= region.xMin && x <= region.xMax && y >= region.yMin && y <= region.yMax;
        }

        public static bool CanStartDrag(
              Vector2 position
            , Vector2 screenSize
            , Rect region
            , Func<Vector2, bool> isBlocked
        )
        {
            return IsInRegion(position, screenSize, region) && (isBlocked == null || isBlocked(position) == false);
        }

        public static Vector2 GetInput(Vector2 origin, Vector2 position, float radius)
        {
            return radius <= 0f ? Vector2.zero : Vector2.ClampMagnitude((position - origin) / radius, 1f);
        }

        public static Vector2 ApplyDeadZone(Vector2 input)
        {
            return input.magnitude < DEAD_ZONE ? Vector2.zero : Vector2.ClampMagnitude(input, 1f);
        }
    }
}
