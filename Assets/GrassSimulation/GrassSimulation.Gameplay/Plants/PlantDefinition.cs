using System;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [Serializable]
    public struct PlantDefinition
    {
        [field: SerializeField]
        public PlantKind Kind { get; set; }

        [field: SerializeField]
        public string Key { get; set; }

        [field: SerializeField]
        public PlantRepresentation Representation { get; set; }

        [field: SerializeField]
        public PlantShape Shape { get; set; }

        [field: SerializeField]
        public bool IsProtected { get; set; }

        [field: SerializeField]
        public int RequiredTier { get; set; }

        [field: SerializeField]
        public float Toughness { get; set; }

        [field: SerializeField]
        public float CutZoneRadius { get; set; }

        [field: SerializeField]
        public int Xp { get; set; }

        [field: SerializeField]
        public int FruitCount { get; set; }

        [field: SerializeField]
        public bool IsFruit { get; set; }

        [field: SerializeField]
        public Color EffectColor { get; set; }

        [field: SerializeField]
        public GameObject[] ObjectPrefabs { get; set; }

        [field: SerializeField]
        public Sprite Icon { get; set; }

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
