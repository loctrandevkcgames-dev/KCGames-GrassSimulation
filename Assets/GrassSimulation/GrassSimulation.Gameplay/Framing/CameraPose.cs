using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public readonly record struct CameraPose(Vector3 Focus, float OrthoSize)
    {
        public static CameraPose Lerp(in CameraPose from, in CameraPose to, float t)
        {
            var clamped = Mathf.Clamp01(t);

            return new CameraPose(
                  Vector3.Lerp(from.Focus, to.Focus, clamped)
                , Mathf.Lerp(from.OrthoSize, to.OrthoSize, clamped)
            );
        }
    }
}
