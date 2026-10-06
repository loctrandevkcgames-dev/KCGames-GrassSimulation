using UnityEngine;

namespace GrassSimulation.UI
{
    public static class UpgradeVisuals
    {
        public const string WIDE_BLADE = "WideBlade";
        public const string STRONG_ENGINE = "StrongEngine";

        private const float WIDE_BLADE_ICON_SIZE = 84f * UiMetrics.SCALE;
        private const float STRONG_ENGINE_ICON_SIZE = 46f * UiMetrics.SCALE;
        private const float GENERIC_ICON_SIZE = 46f * UiMetrics.SCALE;

        private static readonly UpgradeVisual s_wideBlade = new(
              UiText.UPGRADE_WIDE_BLADE
            , true
            , UiText.UPGRADE_WIDE_BLADE_HINT
            , UpgradeIcon.Rings
            , new Color32(r: 0xE3, g: 0xEE, b: 0xDC, a: 0xFF)
            , UiPalette.White
            , WIDE_BLADE_ICON_SIZE
            , UpgradeStatKinds.CutRadius
            , true
        );

        private static readonly UpgradeVisual s_strongEngine = new(
              UiText.UPGRADE_STRONG_ENGINE
            , true
            , UiText.UPGRADE_STRONG_ENGINE_HINT
            , UpgradeIcon.Bolt
            , new Color32(r: 0xFF, g: 0xF3, b: 0xD6, a: 0xFF)
            , new Color32(r: 0xB9, g: 0x83, b: 0x1C, a: 0xFF)
            , STRONG_ENGINE_ICON_SIZE
            , UpgradeStatKinds.CuttingPower | UpgradeStatKinds.Speed
            , true
        );

        public static UpgradeVisual Get(string id)
        {
            return id switch {
                WIDE_BLADE => s_wideBlade,
                STRONG_ENGINE => s_strongEngine,
                _ => new UpgradeVisual(
                      id ?? string.Empty
                    , false
                    , string.Empty
                    , UpgradeIcon.Bolt
                    , UiPalette.Soft
                    , UiPalette.Primary
                    , GENERIC_ICON_SIZE
                    , UpgradeStatKinds.CutRadius | UpgradeStatKinds.CuttingPower | UpgradeStatKinds.Speed
                    , false
                ),
            };
        }
    }
}
