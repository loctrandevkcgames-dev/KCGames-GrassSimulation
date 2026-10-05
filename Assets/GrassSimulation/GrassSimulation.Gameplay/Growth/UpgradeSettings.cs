using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct UpgradeSettings
    {
        [field: SerializeField]
        public string Id { get; set; }

        [field: SerializeField]
        public float CutRadiusDelta { get; set; }

        [field: SerializeField]
        public float CuttingPowerDelta { get; set; }

        [field: SerializeField]
        public float SpeedDelta { get; set; }
    }
}
