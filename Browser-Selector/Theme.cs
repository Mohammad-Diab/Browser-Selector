using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace BrowserSelector
{
    /// <summary>Light or dark palette, following the Windows app theme.</summary>
    public static class Theme
    {
        public static bool IsDark { get; private set; }

        public static void Apply(ResourceDictionary res)
        {
            IsDark = SystemUsesDark();
            void Set(string key, string light, string dark)
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(IsDark ? dark : light));
                brush.Freeze();
                res[key] = brush;
            }
            Set("WindowBg", "#F3F3F3", "#202020");
            Set("CardBg", "#FFFFFF", "#2B2B2B");
            Set("Fg", "#1B1B1B", "#FFFFFF");
            Set("Muted", "#5F5F5F", "#A8A8A8");
            Set("Line", "#E0E0E0", "#3B3B3B");
            Set("Hover", "#F0F0F0", "#353535");
            Set("Pressed", "#E6E6E6", "#3D3D3D");
            Set("Selected", "#E1EEFA", "#203547");
            Set("Accent", "#005FB8", "#60CDFF");
            Set("AccentHover", "#1A6FC0", "#56B9E6");
            Set("OnAccent", "#FFFFFF", "#000000");
            Set("Badge", "#EAEAEA", "#3A3A3A");
            Set("InputBg", "#FFFFFF", "#2D2D2D");
            Set("Danger", "#C42B1C", "#FF99A4");
            Set("Ok", "#0F7B0F", "#6CCB5F");
        }

        static bool SystemUsesDark()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
            }
            catch { return false; }
        }

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        const int DWMWCP_ROUND = 2;

        /// <summary>Dark title bar and (on Windows 11) rounded corners. Call from SourceInitialized.</summary>
        public static void StyleWindow(Window w, bool roundCorners)
        {
            var hwnd = new WindowInteropHelper(w).Handle;
            try
            {
                int dark = IsDark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
                if (roundCorners)
                {
                    int round = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
                }
            }
            catch { /* older Windows: keep the defaults */ }
        }
    }
}
