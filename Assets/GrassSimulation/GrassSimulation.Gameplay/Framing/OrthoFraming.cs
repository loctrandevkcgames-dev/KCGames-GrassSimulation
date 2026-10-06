using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public static class OrthoFraming
    {
        private const float MIN_PITCH_SIN = 0.05f;
        private const float MIN_VIEWPORT_SIZE = 0.01f;

        public static bool TryFit(
              Rect groundXZ
            , float pitchDegrees
            , float aspect
            , Rect viewport
            , float margin
            , float heightAllowance
            , out CameraPose pose
        )
        {
            pose = default;

            var pitch = pitchDegrees * Mathf.Deg2Rad;
            var sin = Mathf.Sin(pitch);
            var cos = Mathf.Cos(pitch);

            if (sin < MIN_PITCH_SIN
                || aspect <= 0f
                || viewport.width < MIN_VIEWPORT_SIZE
                || viewport.height < MIN_VIEWPORT_SIZE
            )
            {
                return false;
            }

            var uMin = groundXZ.xMin - margin;
            var uMax = groundXZ.xMax + margin;
            var vMin = (groundXZ.yMin - margin) * sin;
            var vMax = (groundXZ.yMax + margin) * sin + heightAllowance * cos;

            var sizeByHeight = (vMax - vMin) / (2f * viewport.height);
            var sizeByWidth = (uMax - uMin) / (2f * aspect * viewport.width);
            var size = Mathf.Max(sizeByHeight, sizeByWidth);

            var du = (viewport.center.x - 0.5f) * 2f * size * aspect;
            var dv = (viewport.center.y - 0.5f) * 2f * size;
            var u = (uMin + uMax) * 0.5f - du;
            var v = (vMin + vMax) * 0.5f - dv;

            pose = new CameraPose(new Vector3(x: u, y: 0f, z: v / sin), size);
            return true;
        }
    }
}
