using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "BoosterConfig", menuName = "Grass Simulation/Booster Config")]
    public sealed class BoosterConfig : ScriptableObject
    {
        [SerializeField]
        private float _turboSeconds = 8f;

        [SerializeField]
        private float _turboSpeedMultiplier = 1.25f;

        [SerializeField]
        private float _turboPowerMultiplier = 1.5f;

        [SerializeField]
        private float _extraTimeSeconds = 15f;

        public BoosterValues Values => new(
              _turboSeconds
            , _turboSpeedMultiplier
            , _turboPowerMultiplier
            , _extraTimeSeconds
        );
    }
}
