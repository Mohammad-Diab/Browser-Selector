using System;
using System.Runtime.InteropServices;
using System.Text;

namespace BrowserSelector
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        public const uint MONITOR_DEFAULTTONEAREST = 2;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const int SHCNE_ASSOCCHANGED = 0x08000000;
        public const uint SHCNF_IDLIST = 0x0000;

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT pt);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public const uint GW_OWNER = 4;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);

        public const uint EVENT_SYSTEM_FOREGROUND = 3;
        public const uint WINEVENT_OUTOFCONTEXT = 0;

        public delegate void WinEventProc(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr module, WinEventProc proc,
            uint processId, uint threadId, uint flags);

        [DllImport("user32.dll")]
        public static extern bool UnhookWinEvent(IntPtr hook);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int key);

        /// <summary>Whether Shift is held right now (works before any window exists).</summary>
        public static bool IsShiftDown() => (GetAsyncKeyState(0x10) & 0x8000) != 0;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern uint PrivateExtractIcons(string file, int index, int cx, int cy,
            IntPtr[] icons, uint[] ids, uint count, uint flags);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr icon);

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int SHLoadIndirectString(string source, StringBuilder output, int size, IntPtr reserved);

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);

        /// <summary>Resolves "@file.dll,-123" style strings; returns the input when it isn't one.</summary>
        public static string ResolveIndirect(string value)
        {
            if (string.IsNullOrEmpty(value) || value[0] != '@') return value;
            var sb = new StringBuilder(512);
            return SHLoadIndirectString(value, sb, sb.Capacity, IntPtr.Zero) == 0 ? sb.ToString() : value;
        }
    }
}
