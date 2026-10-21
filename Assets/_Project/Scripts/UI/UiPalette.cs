using UnityEngine;

namespace DeadAir.UI
{
    // Цвета интерфейса из гайда проекта. Красный #D63D2B закреплён за угрозой,
    // поэтому в инвентаре его нет: перегруз — не угроза.
    public static class UiPalette
    {
        public static readonly Color Text = Hex(0xE5E0D4);
        public static readonly Color Muted = Hex(0x8C8578);
        public static readonly Color Amber = Hex(0xEBA138);
        public static readonly Color Panel = Hex(0x22201D);
        public static readonly Color Background = Hex(0x171413);
        // Выбранная строка на панели: на ступень светлее #22201D.
        public static readonly Color PanelRaised = Hex(0x2E2B27);

        public static Color WithAlpha(this Color color, float alpha) => new(color.r, color.g, color.b, alpha);

        private static Color Hex(int rgb) =>
            new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
