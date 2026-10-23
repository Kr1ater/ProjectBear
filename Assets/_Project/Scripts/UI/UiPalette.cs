using UnityEngine;

namespace DeadAir.UI
{
    // Цвета интерфейса из гайда проекта.
    // Красный #D63D2B закреплён за угрозой, поэтому в инвентаре его нет: перегруз — не угроза.
    public static class UiPalette
    {
        public static readonly Color Text = FromHex("#E5E0D4");
        public static readonly Color Muted = FromHex("#8C8578");
        public static readonly Color Amber = FromHex("#EBA138");
        // Выбранная строка в списке: на ступень светлее панели (#22201D).
        public static readonly Color PanelRaised = FromHex("#2E2B27");

        // Тот же цвет, но с другой прозрачностью: 0 — невидимый, 1 — непрозрачный.
        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color FromHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
