using UnityEngine;

namespace GrassSimulation.UI
{
    public static class UiPalette
    {
        public static readonly Color Ink = new Color32(r: 0x1B, g: 0x2E, b: 0x1F, a: 0xFF);
        public static readonly Color Primary = new Color32(r: 0x2E, g: 0x7D, b: 0x32, a: 0xFF);
        public static readonly Color Sun = new Color32(r: 0xF4, g: 0xB7, b: 0x40, a: 0xFF);
        public static readonly Color Soft = new Color32(r: 0xE4, g: 0xEF, b: 0xD6, a: 0xFF);
        public static readonly Color Muted = new Color32(r: 0x4A, g: 0x5D, b: 0x4E, a: 0xFF);
        public static readonly Color Warning = new Color32(r: 0xE8, g: 0x74, b: 0x3B, a: 0xFF);
        public static readonly Color Protected = new Color32(r: 0x2F, g: 0x6F, b: 0xD6, a: 0xFF);
        public static readonly Color ProtectedFill = new Color32(r: 0xE5, g: 0xEE, b: 0xFB, a: 0xFF);
        public static readonly Color ProtectedInk = new Color32(r: 0x1B, g: 0x3F, b: 0x7A, a: 0xFF);
        public static readonly Color White = Color.white;
        public static readonly Color PanelTranslucent = new Color(r: 1f, g: 1f, b: 1f, a: 0.92f);
        public static readonly Color Shadow = new Color(r: 0x1B / 255f, g: 0x2E / 255f, b: 0x1F / 255f, a: 0.25f);
    }
}
