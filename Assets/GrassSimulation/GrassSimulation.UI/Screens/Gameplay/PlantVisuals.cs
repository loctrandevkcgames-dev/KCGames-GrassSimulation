using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.UI
{
    public static class PlantVisuals
    {
        private static readonly PlantChipColors s_grassColors = new(
              UiPalette.Soft
            , new Color32(r: 0x1D, g: 0x54, b: 0x20, a: 0xFF)
            , UiPalette.Primary
            , new Color32(r: 0xD2, g: 0xE3, b: 0xBF, a: 0xFF)
        );

        private static readonly PlantChipColors s_flowerColors = new(
              new Color32(r: 0xFC, g: 0xE8, b: 0xE0, a: 0xFF)
            , new Color32(r: 0x8A, g: 0x2F, b: 0x12, a: 0xFF)
            , new Color32(r: 0xC2, g: 0x45, b: 0x2A, a: 0xFF)
            , new Color32(r: 0xF3, g: 0xE4, b: 0xDD, a: 0xFF)
        );

        private static readonly PlantChipColors s_bushColors = new(
              new Color32(r: 0xEF, g: 0xE3, b: 0xCF, a: 0xFF)
            , new Color32(r: 0x6B, g: 0x4F, b: 0x2A, a: 0xFF)
            , new Color32(r: 0x9C, g: 0x7A, b: 0x50, a: 0xFF)
            , new Color32(r: 0xEC, g: 0xE3, b: 0xD4, a: 0xFF)
        );

        public static PlantIcon GetIcon(PlantKind kind)
        {
            return kind switch {
                PlantKind.HarvestFlower => PlantIcon.Flower,
                PlantKind.ProtectedFlower => PlantIcon.Flower,
                PlantKind.LowBush => PlantIcon.Bush,
                PlantKind.HardBush => PlantIcon.Bush,
                PlantKind.BushLow => PlantIcon.Bush,
                PlantKind.Vegetable => PlantIcon.Bush,
                PlantKind.BushBig => PlantIcon.Bush,
                PlantKind.Melon => PlantIcon.Bush,
                PlantKind.FruitTree => PlantIcon.Bush,
                PlantKind.GiantFruit => PlantIcon.Bush,
                _ => PlantIcon.Grass,
            };
        }

        public static string GetLabel(PlantKind kind)
        {
            return kind switch {
                PlantKind.HarvestFlower => UiText.PLANT_FLOWER,
                PlantKind.ThickGrass => UiText.PLANT_THICK_GRASS,
                PlantKind.LowBush => UiText.PLANT_LOW_BUSH,
                PlantKind.HardBush => UiText.PLANT_HARD_BUSH,
                PlantKind.ProtectedFlower => UiText.PLANT_PROTECTED_FLOWER,
                PlantKind.BushLow => UiText.PLANT_BUSH_LOW,
                PlantKind.Vegetable => UiText.PLANT_VEGETABLE,
                PlantKind.BushBig => UiText.PLANT_BUSH_BIG,
                PlantKind.Melon => UiText.PLANT_MELON,
                PlantKind.FruitTree => UiText.PLANT_FRUIT_TREE,
                PlantKind.GiantFruit => UiText.PLANT_GIANT_FRUIT,
                _ => UiText.PLANT_GRASS,
            };
        }

        public static PlantChipColors GetColors(PlantKind kind)
        {
            return GetIcon(kind) switch {
                PlantIcon.Flower => s_flowerColors,
                PlantIcon.Bush => s_bushColors,
                _ => s_grassColors,
            };
        }
    }
}
