using System;
using System.Linq;
using System.Windows;

namespace BrowserSelector
{
    /// <summary>
    /// BrowserSelector.exe &lt;link&gt;   → open by site rule, else in the main browser or the picker (Shift: picker)
    /// BrowserSelector.exe          → settings
    /// BrowserSelector.exe --register | --unregister   → change the registration without UI
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var settings = AppSettings.Load();
            Loc.Init(settings.Language);
            Theme.Apply(Resources);

            var args = e.Args.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
            if (args.Length == 1 && (args[0] == "--register" || args[0] == "--unregister"))
            {
                try
                {
                    if (args[0] == "--register") Registration.Register(BrowserCatalog.SelfPath);
                    else Registration.Unregister();
                    Shutdown(0);
                }
                catch { Shutdown(1); }
                return;
            }

            if (args.Length == 0)
            {
                new SettingsWindow().Show();
                return;
            }

            // Windows passes the link as one argument; join in case a caller didn't quote it.
            var input = string.Join(" ", args);
            if (!Launcher.TryNormalize(input, out var link))
            {
                MessageBox.Show(Loc.F("Error.BadLink", input), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown(1);
                return;
            }

            var browsers = BrowserCatalog.Discover();
            // Holding Shift while opening a link always shows the picker.
            var option = Native.IsShiftDown() ? null : Launcher.Route(link, browsers, settings, out _, out _);
            if (option != null)
            {
                try
                {
                    Launcher.Open(option, link);
                    Shutdown(0);
                    return;
                }
                catch { /* the browser failed to start: let the user pick another one */ }
            }
            new PickerWindow(link, browsers, settings).Show();
        }
    }
}
