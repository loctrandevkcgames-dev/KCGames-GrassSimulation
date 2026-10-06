using UnityEngine;

namespace GrassSimulation.UI
{
    public static class JoystickLayout
    {
        public const float BASE_DESIGN_SIZE = 132f;
        public const float KNOB_DESIGN_SIZE = 60f;
        public const float KNOB_TO_BASE_RATIO = KNOB_DESIGN_SIZE / BASE_DESIGN_SIZE;

        private const float DIAMETER_PER_RADIUS = 2f;

        public static float GetUnitsPerPixel(float canvasWidth, float screenWidth)
        {
            return screenWidth <= 0f ? 0f : canvasWidth / screenWidth;
        }

        public static Vector2 ToCanvasPosition(Vector2 screenPosition, Vector2 screenSize, Vector2 canvasSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                return Vector2.zero;
            }

            return new Vector2(
                  screenPosition.x / screenSize.x * canvasSize.x
                , screenPosition.y / screenSize.y * canvasSize.y
            );
        }

        public static float GetBaseSize(float radiusPixels, float unitsPerPixel)
        {
            return radiusPixels * DIAMETER_PER_RADIUS * unitsPerPixel;
        }

        public static float GetKnobSize(float radiusPixels, float unitsPerPixel)
        {
            return GetBaseSize(radiusPixels, unitsPerPixel) * KNOB_TO_BASE_RATIO;
        }

        public static Vector2 GetKnobOffset(Vector2 input, float radiusPixels, float unitsPerPixel)
        {
            return input * (radiusPixels * unitsPerPixel);
        }
    }
}
