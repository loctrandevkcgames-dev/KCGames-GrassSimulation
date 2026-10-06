using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public struct CameraBlend
    {
        public float Weight { get; private set; }

        public readonly float Eased => Weight * Weight * (3f - 2f * Weight);

        public void Step(float target, float seconds, float deltaTime)
        {
            if (seconds <= 0f)
            {
                Snap(target);
                return;
            }

            Weight = Mathf.MoveTowards(Weight, Mathf.Clamp01(target), deltaTime / seconds);
        }

        public void Snap(float target)
        {
            Weight = Mathf.Clamp01(target);
        }
    }
}
