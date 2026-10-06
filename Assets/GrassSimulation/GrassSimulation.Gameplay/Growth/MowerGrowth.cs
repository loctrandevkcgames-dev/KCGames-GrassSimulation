using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class MowerGrowth
    {
        private readonly MachineConfig _config;
        private readonly int[] _upgradeCounts;

        public MowerGrowth(MachineConfig config)
        {
            _config = config;
            _upgradeCounts = new int[config.Upgrades.Length];
            Reset();
        }

        public int Tier { get; private set; }

        public int Xp { get; private set; }

        public int PendingUpgrades { get; private set; }

        public int UpgradeOptionCount => _upgradeCounts.Length;

        public int UpgradeTier => PendingUpgrades > 0 ? Tier - PendingUpgrades + 1 : Tier;

        public MachineStats Stats => ComputeStats(option: 0, extraTimes: 0);

        public int TierFloorXp
        {
            get
            {
                var thresholds = _config.XpThresholds;
                var index = Tier - 2;

                return (uint)index < (uint)thresholds.Length ? thresholds[index] : 0;
            }
        }

        public MachineStats GetStatsWithUpgrade(int option)
            => ComputeStats(option, extraTimes: 1);

        public bool TryGetNextThreshold(out int threshold)
        {
            var thresholds = _config.XpThresholds;
            var index = Tier - 1;

            if (index < thresholds.Length)
            {
                threshold = thresholds[index];
                return true;
            }

            threshold = default;
            return false;
        }

        public ref readonly UpgradeSettings GetUpgrade(int option)
            => ref _config.Upgrades[option];

        public int GetUpgradeCount(int option)
            => _upgradeCounts[option];

        public void Reset()
        {
            Array.Clear(_upgradeCounts, 0, _upgradeCounts.Length);
            Tier = 1;
            Xp = 0;
            PendingUpgrades = 0;
        }

        public int AddXp(int xp)
        {
            var tiersGained = 0;
            Xp += xp;

            while (TryGetNextThreshold(out var threshold) && Xp >= threshold)
            {
                Tier++;
                PendingUpgrades++;
                tiersGained++;
            }

            return tiersGained;
        }

        public bool TryChooseUpgrade(int option)
        {
            var isValidOption = (uint)option < (uint)_upgradeCounts.Length;

            if (PendingUpgrades == 0 || isValidOption == false)
            {
                return false;
            }

            _upgradeCounts[option]++;
            PendingUpgrades--;
            return true;
        }

        private MachineStats ComputeStats(int option, int extraTimes)
        {
            var upgrades = _config.Upgrades;
            var cutRadius = _config.BaseCutRadius;
            var cuttingPower = _config.BaseCuttingPower;
            var speed = _config.BaseSpeed;
            var count = _upgradeCounts.Length;

            for (var i = 0; i < count; i++)
            {
                var upgrade = upgrades[i];
                var times = _upgradeCounts[i] + (i == option ? extraTimes : 0);
                cutRadius += upgrade.CutRadiusDelta * times;
                cuttingPower += upgrade.CuttingPowerDelta * times;
                speed += upgrade.SpeedDelta * times;
            }

            return new MachineStats(cutRadius, cuttingPower, speed);
        }
    }
}
