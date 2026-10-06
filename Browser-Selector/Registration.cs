using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace BrowserSelector
{
    /// <summary>
    /// Registers the app as a browser for the current user (HKCU only), the way Windows 10/11 expects:
    /// a StartMenuInternet client with Capabilities, a RegisteredApplications entry and ProgIDs.
    /// Windows does not let apps make themselves the default; the user picks it in Settings.
    /// </summary>
    public static class Registration
    {
        public const string ClientKeyName = "BrowserSelector";
        public const string AppName = "Browser Selector";
        public const string UrlProgId = "BrowserSelectorURL";
        public const string HtmlProgId = "BrowserSelectorHTML";

        const string Description = "Asks which browser to open each link with.";
        const string ClientKey = @"Software\Clients\StartMenuInternet\" + ClientKeyName;
        const string CapabilitiesKey = ClientKey + @"\Capabilities";
        const string RegisteredApps = @"Software\RegisteredApplications";
        static readonly string[] Protocols = { "http", "https" };
        static readonly string[] Extensions = { ".htm", ".html" };

        public enum Status { NotRegistered, Registered, OtherPath }

        public static Status GetStatus(string exe)
        {
            using (var cmd = Registry.CurrentUser.OpenSubKey(@"Software\Classes\" + UrlProgId + @"\shell\open\command"))
            using (var apps = Registry.CurrentUser.OpenSubKey(RegisteredApps))
            {
                var value = cmd?.GetValue("") as string;
                if (value == null || apps?.GetValue(AppName) == null) return Status.NotRegistered;
                return BrowserCatalog.SplitCommand(value, out var registered, out _)
                       && string.Equals(Path.GetFullPath(registered), Path.GetFullPath(exe), StringComparison.OrdinalIgnoreCase)
                    ? Status.Registered
                    : Status.OtherPath;
            }
        }

        /// <summary>True when Windows currently sends web links to this app.</summary>
        public static bool IsDefault()
        {
            foreach (var protocol in Protocols)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    $@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\{protocol}\UserChoice"))
                {
                    if (!string.Equals(key?.GetValue("ProgId") as string, UrlProgId, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
            }
            return true;
        }

        public static void Register(string exe)
        {
            var icon = $"\"{exe}\",0";
            var open = $"\"{exe}\" \"%1\"";

            WriteProgId(UrlProgId, AppName + " URL", icon, open, isUrl: true);
            WriteProgId(HtmlProgId, AppName + " HTML Document", icon, open, isUrl: false);

            using (var client = Registry.CurrentUser.CreateSubKey(ClientKey))
            {
                client.SetValue("", AppName);
                using (var k = client.CreateSubKey("DefaultIcon")) k.SetValue("", icon);
                // Opening the app from the Start menu "browser" entry shows its settings.
                using (var k = client.CreateSubKey(@"shell\open\command")) k.SetValue("", $"\"{exe}\"");
                using (var k = client.CreateSubKey("InstallInfo")) k.SetValue("IconsVisible", 1, RegistryValueKind.DWord);
            }
            using (var caps = Registry.CurrentUser.CreateSubKey(CapabilitiesKey))
            {
                caps.SetValue("ApplicationName", AppName);
                caps.SetValue("ApplicationDescription", Description);
                caps.SetValue("ApplicationIcon", icon);
                using (var k = caps.CreateSubKey("URLAssociations"))
                    foreach (var p in Protocols) k.SetValue(p, UrlProgId);
                using (var k = caps.CreateSubKey("FileAssociations"))
                    foreach (var e in Extensions) k.SetValue(e, HtmlProgId);
                using (var k = caps.CreateSubKey("StartMenu")) k.SetValue("StartMenuInternet", ClientKeyName);
            }
            using (var apps = Registry.CurrentUser.CreateSubKey(RegisteredApps))
                apps.SetValue(AppName, CapabilitiesKey);
            foreach (var e in Extensions)
                using (var k = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{e}\OpenWithProgids"))
                    k.SetValue(HtmlProgId, new byte[0], RegistryValueKind.None);

            NotifyShell();
        }

        public static void Unregister()
        {
            var hkcu = Registry.CurrentUser;
            hkcu.DeleteSubKeyTree(@"Software\Classes\" + UrlProgId, false);
            hkcu.DeleteSubKeyTree(@"Software\Classes\" + HtmlProgId, false);
            hkcu.DeleteSubKeyTree(ClientKey, false);
            using (var apps = hkcu.OpenSubKey(RegisteredApps, true))
                apps?.DeleteValue(AppName, false);
            foreach (var e in Extensions)
                using (var k = hkcu.OpenSubKey($@"Software\Classes\{e}\OpenWithProgids", true))
                    k?.DeleteValue(HtmlProgId, false);

            NotifyShell();
        }

        /// <summary>Opens Settings > Default apps, on this app's page where Windows supports it.</summary>
        public static void OpenDefaultAppsSettings()
        {
            Process.Start(new ProcessStartInfo("ms-settings:defaultapps?registeredAppUser=" + Uri.EscapeDataString(AppName))
            {
                UseShellExecute = true
            });
        }

        static void WriteProgId(string progId, string friendlyName, string icon, string open, bool isUrl)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + progId))
            {
                key.SetValue("", friendlyName);
                key.SetValue("FriendlyTypeName", friendlyName);
                if (isUrl) key.SetValue("URL Protocol", "");
                using (var k = key.CreateSubKey("DefaultIcon")) k.SetValue("", icon);
                using (var k = key.CreateSubKey(@"shell\open\command")) k.SetValue("", open);
                using (var app = key.CreateSubKey("Application"))
                {
                    app.SetValue("ApplicationName", AppName);
                    app.SetValue("ApplicationIcon", icon);
                    app.SetValue("ApplicationDescription", Description);
                    app.SetValue("ApplicationCompany", "Mohammad Diab");
                }
            }
        }

        static void NotifyShell() =>
            Native.SHChangeNotify(Native.SHCNE_ASSOCCHANGED, Native.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }
}
