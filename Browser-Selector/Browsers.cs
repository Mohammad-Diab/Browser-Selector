using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace BrowserSelector
{
    public enum BrowserFamily { Other, Chromium, Firefox }

    public sealed class BrowserProfile
    {
        // Ids stay the same when a profile is renamed. Chromium: the profile directory ("Profile 1").
        // Firefox: the Path value from profiles.ini ("Profiles/abcd.default-release").
        public string Id { get; set; }
        public string Name { get; set; }
        /// <summary>Firefox: the profile's full folder, opened with -profile.</summary>
        public string Folder { get; set; }
    }

    public sealed class Browser
    {
        public string Id { get; set; }    // key name under StartMenuInternet
        public string Name { get; set; }
        public string ExePath { get; set; }
        public string ExtraArgs { get; set; }
        public string IconFile { get; set; }
        public int IconIndex { get; set; }
        public BrowserFamily Family { get; set; }
        public string PrivateArg { get; set; }
        List<BrowserProfile> profiles;

        /// <summary>Read on first use, so opening a link by a rule doesn't parse every browser's profile files.</summary>
        public List<BrowserProfile> Profiles
        {
            get
            {
                if (profiles == null)
                {
                    profiles = new List<BrowserProfile>();
                    BrowserCatalog.LoadProfiles(this);
                }
                return profiles;
            }
        }

        ImageSource icon;
        bool iconLoaded;

        public ImageSource Icon
        {
            get
            {
                if (!iconLoaded)
                {
                    iconLoaded = true;
                    icon = LoadIcon(IconFile, IconIndex) ?? LoadIcon(ExePath, 0);
                }
                return icon;
            }
        }

        static ImageSource LoadIcon(string file, int index)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return null;
            var handles = new IntPtr[1];
            var ids = new uint[1];
            try
            {
                if (Native.PrivateExtractIcons(file, index, 48, 48, handles, ids, 1, 0) == 0 || handles[0] == IntPtr.Zero)
                    return null;
                var src = Imaging.CreateBitmapSourceFromHIcon(handles[0], Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                return src;
            }
            catch { return null; }
            finally
            {
                if (handles[0] != IntPtr.Zero) Native.DestroyIcon(handles[0]);
            }
        }
    }

    /// <summary>Finds the browsers registered with Windows and their profiles.</summary>
    public static class BrowserCatalog
    {
        static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        static readonly string RoamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Executables that are not real choices (IE only redirects to Edge on Windows 11).
        static readonly HashSet<string> Ignored = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "iexplore.exe" };

        static readonly Dictionary<string, string> ChromiumPrivate = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["chrome.exe"] = "--incognito",
            ["chromium.exe"] = "--incognito",
            ["brave.exe"] = "--incognito",
            ["vivaldi.exe"] = "--incognito",
            ["thorium.exe"] = "--incognito",
            ["browser.exe"] = "--incognito", // Yandex
            ["msedge.exe"] = "--inprivate",
            ["opera.exe"] = "--private",
            ["launcher.exe"] = "--private",   // Opera / Opera GX
        };

        // Firefox-based browsers and where their profiles.ini lives under %APPDATA%.
        static readonly Dictionary<string, string> FirefoxData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["firefox.exe"] = @"Mozilla\Firefox",
            ["librewolf.exe"] = "librewolf",
            ["waterfox.exe"] = "Waterfox",
            ["floorp.exe"] = "Floorp",
            ["zen.exe"] = "zen",
        };

        public static List<Browser> Discover()
        {
            var self = SelfPath;
            var result = new List<Browser>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sources = new[]
            {
                (RegistryHive.CurrentUser, RegistryView.Default),
                (RegistryHive.LocalMachine, RegistryView.Registry64),
                (RegistryHive.LocalMachine, RegistryView.Registry32),
            };
            foreach (var (hive, view) in sources)
            {
                try
                {
                    using (var root = RegistryKey.OpenBaseKey(hive, view))
                    using (var smi = root.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet"))
                    {
                        if (smi == null) continue;
                        foreach (var id in smi.GetSubKeyNames())
                        {
                            using (var key = smi.OpenSubKey(id))
                            {
                                var b = Read(id, key);
                                if (b == null) continue;
                                if (string.Equals(b.ExePath, self, StringComparison.OrdinalIgnoreCase)) continue;
                                if (string.Equals(id, Registration.ClientKeyName, StringComparison.OrdinalIgnoreCase)) continue;
                                if (Ignored.Contains(Path.GetFileName(b.ExePath))) continue;
                                if (!seen.Add(b.ExePath)) continue;
                                result.Add(b);
                            }
                        }
                    }
                }
                catch { /* a broken hive should not hide the others */ }
            }
            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        public static string SelfPath => System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        static Browser Read(string id, RegistryKey key)
        {
            if (key == null) return null;
            string command;
            using (var cmd = key.OpenSubKey(@"shell\open\command"))
                command = cmd?.GetValue("") as string;
            if (!SplitCommand(command, out var exe, out var args) || !File.Exists(exe)) return null;

            string name = key.GetValue("") as string;
            if (string.IsNullOrWhiteSpace(name))
                using (var caps = key.OpenSubKey("Capabilities"))
                    name = caps?.GetValue("ApplicationName") as string;
            name = Native.ResolveIndirect(name);
            if (string.IsNullOrWhiteSpace(name)) name = id;

            string iconFile = exe;
            int iconIndex = 0;
            using (var di = key.OpenSubKey("DefaultIcon"))
                ParseIcon(di?.GetValue("") as string, ref iconFile, ref iconIndex);

            var file = Path.GetFileName(exe);
            var b = new Browser { Id = id, Name = name.Trim(), ExePath = exe, ExtraArgs = args, IconFile = iconFile, IconIndex = iconIndex };
            if (ChromiumPrivate.TryGetValue(file, out var p)) { b.Family = BrowserFamily.Chromium; b.PrivateArg = p; }
            else if (FirefoxData.ContainsKey(file)) { b.Family = BrowserFamily.Firefox; b.PrivateArg = "-private-window"; }
            return b;
        }

        /// <summary>Splits a registry command line into the executable and the arguments after it.</summary>
        public static bool SplitCommand(string command, out string exe, out string args)
        {
            exe = args = "";
            if (string.IsNullOrWhiteSpace(command)) return false;
            command = Environment.ExpandEnvironmentVariables(command.Trim());
            int end;
            if (command[0] == '"')
            {
                end = command.IndexOf('"', 1);
                if (end < 0) return false;
                exe = command.Substring(1, end - 1);
                args = command.Substring(end + 1);
            }
            else
            {
                end = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
                if (end < 0) return false;
                exe = command.Substring(0, end + 4);
                args = command.Substring(end + 4);
            }
            args = CleanArgs(args);
            return exe.Length > 0;
        }

        // Placeholders for the link, and switches that only make sense right before it ("--single-argument %1",
        // "-osint -url %1"). Left in, they would land in front of our own switches and swallow them.
        static readonly HashSet<string> LinkTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "%1", "\"%1\"", "%L", "\"%L\"", "%*", "--single-argument", "-osint", "-url",
        };

        static string CleanArgs(string args) =>
            string.Join(" ", System.Text.RegularExpressions.Regex.Matches(args, "\"[^\"]*\"|\\S+")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value)
                .Where(t => !LinkTokens.Contains(t)));

        static void ParseIcon(string value, ref string file, ref int index)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            value = Environment.ExpandEnvironmentVariables(value.Trim());
            int comma = value.LastIndexOf(',');
            string path = value;
            int idx = 0;
            if (comma > 0 && int.TryParse(value.Substring(comma + 1).Trim(), out var n)) { path = value.Substring(0, comma); idx = n; }
            path = path.Trim().Trim('"');
            if (File.Exists(path)) { file = path; index = idx; }
        }

        internal static void LoadProfiles(Browser b)
        {
            try
            {
                if (b.Family == BrowserFamily.Chromium) LoadChromiumProfiles(b);
                else if (b.Family == BrowserFamily.Firefox) LoadFirefoxProfiles(b);
            }
            catch { /* profiles are optional */ }
        }

        /// <summary>
        /// Chromium keeps "User Data" in %LOCALAPPDATA% under the same vendor\product folders
        /// that hold its "Application" folder (e.g. Google\Chrome, Microsoft\Edge, BraveSoftware\Brave-Browser).
        /// </summary>
        static string ChromiumUserData(string exe)
        {
            var appDir = Path.GetDirectoryName(exe);
            if (!string.Equals(Path.GetFileName(appDir), "Application", StringComparison.OrdinalIgnoreCase)) return null;
            var productDir = Path.GetDirectoryName(appDir);
            if (productDir.StartsWith(LocalAppData + "\\", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(productDir, "User Data");
            foreach (var pf in new[] { Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
            {
                if (string.IsNullOrEmpty(pf) || !productDir.StartsWith(pf + "\\", StringComparison.OrdinalIgnoreCase)) continue;
                return Path.Combine(LocalAppData, productDir.Substring(pf.Length + 1), "User Data");
            }
            return null;
        }

        static void LoadChromiumProfiles(Browser b)
        {
            var dir = ChromiumUserData(b.ExePath);
            var file = dir == null ? null : Path.Combine(dir, "Local State");
            if (file == null || !File.Exists(file)) return;

            var profile = (Json.Parse(ReadShared(file)) as Dictionary<string, object>).Obj("profile");
            var cache = profile.Obj("info_cache");
            if (cache == null) return;

            var order = new List<string>();
            order.AddRange((profile.Arr("profiles_order") ?? new List<object>()).OfType<string>());
            foreach (var k in cache.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
                if (!order.Contains(k)) order.Add(k);

            foreach (var dirName in order)
            {
                if (!(cache.TryGetValue(dirName, out var e) && e is Dictionary<string, object> entry)) continue;
                var name = entry.TryGetValue("name", out var n) ? n as string : null;
                if (string.IsNullOrWhiteSpace(name)) name = dirName;
                b.Profiles.Add(new BrowserProfile { Id = dirName, Name = name });
            }
        }

        static void LoadFirefoxProfiles(Browser b)
        {
            var file = Path.Combine(RoamingAppData, FirefoxData[Path.GetFileName(b.ExePath)], "profiles.ini");
            if (!File.Exists(file)) return;

            var names = new List<string>();
            var installDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // install hash -> path
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // name -> path
            var relative = new HashSet<string>(StringComparer.OrdinalIgnoreCase);       // names with IsRelative=1
            string section = null, name = null, path = null;
            bool isRelative = true;
            void Flush()
            {
                // A profile without a Path can't be opened by folder, and isn't a usable entry.
                if (section != null && section.StartsWith("Profile", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(path))
                {
                    names.Add(name);
                    paths[name] = path;
                    if (isRelative) relative.Add(name);
                }
                name = path = null;
                isRelative = true;
            }
            foreach (var raw in File.ReadAllLines(file))
            {
                var line = raw.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) { Flush(); section = line.Substring(1, line.Length - 2); continue; }
                int eq = line.IndexOf('=');
                if (eq <= 0 || section == null) continue;
                var key = line.Substring(0, eq);
                var val = line.Substring(eq + 1);
                if (key == "Name") name = val;
                else if (key == "Path") path = val;
                else if (key == "IsRelative") isRelative = val.Trim() != "0";
                else if (key == "Default" && section.StartsWith("Install", StringComparison.OrdinalIgnoreCase))
                    installDefaults[section.Substring("Install".Length)] = val;
            }
            Flush();

            // Firefox keeps an unused legacy "default" profile next to "default-release"; hide it.
            if (names.Contains("default-release") && names.Contains("default")
                && !(paths.TryGetValue("default", out var dp) && installDefaults.ContainsValue(dp)))
                names.Remove("default");

            // Each install (Firefox, Developer Edition, ...) shares profiles.ini; the key name ends with the
            // install hash. Hide the profiles that belong to the other installs.
            int dash = b.Id.LastIndexOf('-');
            var hash = dash > 0 ? b.Id.Substring(dash + 1) : null;
            if (hash != null && installDefaults.ContainsKey(hash))
            {
                var others = new HashSet<string>(installDefaults.Where(kv => !kv.Key.Equals(hash, StringComparison.OrdinalIgnoreCase))
                    .Select(kv => kv.Value), StringComparer.OrdinalIgnoreCase);
                others.Remove(installDefaults[hash]);
                names.RemoveAll(n => paths.TryGetValue(n, out var p) && others.Contains(p));
            }

            var iniDir = Path.GetDirectoryName(file);
            foreach (var n in names)
            {
                var p = paths[n];
                var folder = relative.Contains(n) ? Path.Combine(iniDir, p.Replace('/', '\\')) : p;
                b.Profiles.Add(new BrowserProfile { Id = p, Name = n, Folder = folder });
            }
        }

        /// <summary>Reads a file the browser may have open for writing.</summary>
        static string ReadShared(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var sr = new StreamReader(fs))
                return sr.ReadToEnd();
        }
    }
}
