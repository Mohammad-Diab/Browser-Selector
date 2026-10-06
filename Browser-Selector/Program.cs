using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserSelector
{
    /// <summary>
    /// Entry point. A link that a site rule (or the main browser) handles opens here, before any WPF
    /// assembly loads, so it costs almost nothing. Everything else starts the WPF app.
    /// </summary>
    static class Program
    {
        // What Main already worked out, so the app doesn't redo it.
        internal static AppSettings Settings;
        internal static List<Browser> Browsers;
        internal static string Link;
        internal static bool ForcePicker;

        [STAThread]
        static int Main(string[] args)
        {
            var a = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (a.Length > 0 && !a[0].StartsWith("--", StringComparison.Ordinal) && OpenWithoutUi(string.Join(" ", a)))
                return 0;

            var app = new App();
            app.InitializeComponent();
            return app.Run();
        }

        /// <summary>True when the link was opened and nothing needs to be shown.</summary>
        static bool OpenWithoutUi(string input)
        {
            try
            {
                // Holding Shift while opening a link always shows the picker. Read it once, as early as possible.
                ForcePicker = Native.IsShiftDown();
                // Windows passes the link as one argument; joining covers a caller that didn't quote it.
                if (!Launcher.TryNormalize(input, out var link)) return false;
                Link = link;
                Settings = AppSettings.Load();
                Browsers = BrowserCatalog.Discover();
                if (ForcePicker) return false;

                var option = Launcher.Route(link, Browsers, Settings, out _, out _);
                if (option == null) return false;
                Launcher.Open(option, link);
                return true;
            }
            catch
            {
                return false; // the full app handles (and reports) whatever went wrong
            }
        }
    }
}
