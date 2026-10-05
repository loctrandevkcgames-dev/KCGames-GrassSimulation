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

        public MachineStats Stats
        {
            get
            {
                var upgrades = _config.Upgrades;
                var cutRadius = _config.BaseCutRadius;
                var cuttingPower = _config.BaseCuttingPower;
                var speed = _config.BaseSpeed;
                var count = _upgradeCounts.Length;

                for (var i = 0; i < count; i++)
                {
                    var upgrade = upgrades[i];
                    var times = _upgradeCounts[i];
                    cutRadius += upgrade.CutRadiusDelta * times;
                    cuttingPower += upgrade.CuttingPowerDelta * times;
                    speed += upgrade.SpeedDelta * times;
                }

                return new MachineStats(
                      Mathf.Min(cutRadius, _config.MaxCutRadius)
                    , cuttingPower
                    , Mathf.Min(speed, _config.MaxSpeed)
                );
            }
        }

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
    }
}
