using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "MachineConfig", menuName = "Grass Simulation/Machine Config")]
    public sealed class MachineConfig : ScriptableObject
    {
        [SerializeField]
        private float _bodyRadius = 0.3f;

        [SerializeField]
        private float _baseCutRadius = 0.65f;

        [SerializeField]
        private float _maxCutRadius = 1.1f;

        [SerializeField]
        private float _cutRadiusTweenSpeed = 0.6f;

        [SerializeField]
        private float _baseCuttingPower = 1f;

        [SerializeField]
        private float _baseSpeed = 4f;

        [SerializeField]
        private float _maxSpeed = 4.75f;

        [SerializeField]
        private int[] _xpThresholds = { 100, 260, 480 };

        [SerializeField]
        private UpgradeSettings[] _upgrades = Array.Empty<UpgradeSettings>();

        public float BodyRadius => _bodyRadius;

        public float BaseCutRadius => _baseCutRadius;

        public float MaxCutRadius => _maxCutRadius;

        public float CutRadiusTweenSpeed => _cutRadiusTweenSpeed;

        public float BaseCuttingPower => _baseCuttingPower;

        public float BaseSpeed => _baseSpeed;

        public float MaxSpeed => _maxSpeed;

        public ReadOnlySpan<int> XpThresholds => _xpThresholds;

        public ReadOnlySpan<UpgradeSettings> Upgrades => _upgrades;
    }
}
