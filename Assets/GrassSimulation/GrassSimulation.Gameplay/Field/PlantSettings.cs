using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct PlantSettings
    {
        [field: SerializeField]
        public PlantKind Kind { get; set; }

        [field: SerializeField]
        public PlantShape Shape { get; set; }

        [field: SerializeField]
        public bool IsProtected { get; set; }

        [field: SerializeField]
        public int RequiredTier { get; set; }

        [field: SerializeField]
        public float Toughness { get; set; }

        [field: SerializeField]
        public int Xp { get; set; }

        [field: SerializeField]
        public Material Material { get; set; }

        [field: SerializeField]
        public int BladesPerCell { get; set; }

        [field: SerializeField]
        public float MinHeight { get; set; }

        [field: SerializeField]
        public float MaxHeight { get; set; }

        [field: SerializeField]
        public float BladeWidth { get; set; }

        [field: SerializeField]
        public bool HasHead { get; set; }

        [field: SerializeField]
        public Color HeadColor { get; set; }

        [field: SerializeField]
        public float HeadRadius { get; set; }
    }
}
