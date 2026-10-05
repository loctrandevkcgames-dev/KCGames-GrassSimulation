using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct QuotaSettings
    {
        [field: SerializeField]
        public PlantKind Kind { get; set; }

        [field: SerializeField]
        public int Amount { get; set; }

        [field: SerializeField]
        public bool IsBonus { get; set; }
    }
}
