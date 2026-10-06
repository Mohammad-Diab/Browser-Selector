using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

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
            DispatcherUnhandledException += OnUnhandledException;
            var settings = Program.Settings ?? AppSettings.Load();
            Loc.Init(settings.Language);
            Theme.Apply(Resources);

            var args = e.Args.Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
            if (args.Length == 1 && (args[0] == "--register" || args[0] == "--unregister"))
            {
                try
                {
                    if (args[0] == "--register") Registration.Register(BrowserCatalog.SelfPath);
                    else Registration.UnregisterIfOurs(BrowserCatalog.SelfPath);
                    Shutdown(0);
                }
                catch { Shutdown(1); }
                return;
            }

            TellIfSettingsSetAside(null);

            if (args.Length == 0)
            {
                new SettingsWindow().Show();
                return;
            }

            // Program.Main already tried to open the link without UI; here it needs the picker (or an error).
            var input = string.Join(" ", args);
            var link = Program.Link;
            if (link == null && !Launcher.TryNormalize(input, out link))
            {
                MessageBox.Show(Loc.F("Error.BadLink", input), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown(1);
                return;
            }

            var browsers = Program.Browsers ?? BrowserCatalog.Discover();
            var option = Program.ForcePicker ? null : Launcher.Route(link, browsers, settings, out _, out _);
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

        static bool setAsideShown;

        /// <summary>Once per run: say that a broken settings.json was moved aside (kept, not deleted) and defaults are in use.</summary>
        public static void TellIfSettingsSetAside(Window owner)
        {
            if (setAsideShown || AppSettings.SetAsidePath == null) return;
            setAsideShown = true;
            var text = Loc.F("Error.SetAside", AppSettings.SetAsidePath);
            if (owner != null) MessageBox.Show(owner, text, Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            else MessageBox.Show(text, Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>A bug must not lose the link: show the error with the link (Ctrl+C copies the message), then quit.</summary>
        void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            var text = Program.Link == null
                ? Loc.F("Error.Crash", e.Exception.Message)
                : Loc.F("Error.CrashLink", e.Exception.Message, Program.Link);
            try { MessageBox.Show(text, Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error); }
            finally { Shutdown(1); }
        }
    }
}
