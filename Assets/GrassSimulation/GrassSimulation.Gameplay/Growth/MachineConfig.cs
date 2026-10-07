using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [CreateAssetMenu(fileName = "MachineConfig", menuName = "Grass Simulation/Machine Config")]
    public sealed class MachineConfig : ScriptableObject
    {
        [SerializeField]
        private string _id = "standard";

        [SerializeField]
        private string _displayName = string.Empty;

        [SerializeField]
        private string _tradeOff = string.Empty;

        [SerializeField]
        private Sprite _icon;

        [SerializeField]
        private Color _tint = Color.white;

        [SerializeField]
        private float _bodyRadius = 0.3f;

        [SerializeField]
        private float _baseCutRadius = 0.65f;

        [SerializeField]
        private float _cutRadiusTweenSpeed = 0.6f;

        [SerializeField]
        private float _baseCuttingPower = 1f;

        [SerializeField]
        private float _baseSpeed = 4f;

        [SerializeField]
        private int[] _xpThresholds = { 100, 260, 480 };

        [SerializeField]
        private UpgradeSettings[] _upgrades = Array.Empty<UpgradeSettings>();

        public MachineId Id => new(_id);

        public string DisplayName => _displayName;

        public string TradeOff => _tradeOff;

        public Sprite Icon => _icon;

        public Color Tint => _tint;

        public MachineStats BaseStats => new(_baseCutRadius, _baseCuttingPower, _baseSpeed);

        public float BodyRadius => _bodyRadius;

        public float BaseCutRadius => _baseCutRadius;

        public float CutRadiusTweenSpeed => _cutRadiusTweenSpeed;

        public float BaseCuttingPower => _baseCuttingPower;

        public float BaseSpeed => _baseSpeed;

        public ReadOnlySpan<int> XpThresholds => _xpThresholds;

        public ReadOnlySpan<UpgradeSettings> Upgrades => _upgrades;
    }
}
