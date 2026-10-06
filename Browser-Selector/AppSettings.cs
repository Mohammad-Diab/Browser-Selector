using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace BrowserSelector
{
    /// <summary>What to open a link with, as saved in settings.</summary>
    public sealed class Target
    {
        public string Browser { get; set; }   // Browser.Id
        public string Profile { get; set; }   // BrowserProfile.Id, or null
        public bool Private { get; set; }

        internal static Target FromJson(Dictionary<string, object> o) =>
            string.IsNullOrEmpty(o.Str("Browser")) ? null
                : new Target { Browser = o.Str("Browser"), Profile = o.Str("Profile"), Private = o.Bool("Private", false) };

        internal Dictionary<string, object> ToJson() =>
            new Dictionary<string, object> { ["Browser"] = Browser, ["Profile"] = Profile, ["Private"] = Private };
    }

    /// <summary>"Always open this site with ...". Matches the domain and its subdomains.</summary>
    public sealed class SiteRule
    {
        public string Domain { get; set; }
        public Target Target { get; set; }
    }

    public sealed class AppSettings
    {
        public string Language { get; set; } = "auto";   // auto | en | ar
        /// <summary>True: links without a rule show the picker. False: they open in DefaultTarget.</summary>
        public bool AskEveryTime { get; set; } = true;
        /// <summary>The main browser: opened without asking, or highlighted (Enter) in the picker.</summary>
        public Target DefaultTarget { get; set; }
        public bool ShowProfiles { get; set; } = true;
        public List<SiteRule> Rules { get; set; } = new List<SiteRule>();

        /// <summary>The file exists but stayed locked while loading: these are defaults, and must not be saved over it.</summary>
        public bool Unreadable { get; private set; }

        /// <summary>
        /// Set when this process found settings.json broken (say, a typo from editing it by hand) and moved it aside
        /// to this path instead of overwriting it. The UI tells the user.
        /// </summary>
        public static string SetAsidePath { get; private set; }

        const string FileName = "settings.json";

        /// <summary>
        /// %APPDATA%\BrowserSelector\settings.json: Windows' place for per-user app settings, for every copy
        /// (installed or portable). Nothing is ever written next to the exe.
        /// </summary>
        public static string FilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserSelector", FileName);


        /// <summary>
        /// Reads the settings. A missing file gives defaults. A locked file is retried, then gives defaults marked
        /// <see cref="Unreadable"/>. A file that doesn't parse is renamed to settings.json.bad-&lt;time&gt; (kept for
        /// the user to fix) and gives defaults, so a later save can never silently wipe the rules.
        /// </summary>
        public static AppSettings Load()
        {
            var file = FilePath;
            string text = null;
            for (int attempt = 1; text == null; attempt++)
            {
                try
                {
                    if (!File.Exists(file)) return new AppSettings();
                    text = File.ReadAllText(file);
                }
                catch (IOException) when (attempt < 4)
                {
                    Thread.Sleep(80); // another app (an antivirus, OneDrive, another copy of us) has it open
                }
                catch
                {
                    return new AppSettings { Unreadable = true };
                }
            }

            try
            {
                return FromJson(Json.Parse(text) as Dictionary<string, object> ?? throw new FormatException("not a JSON object"));
            }
            catch (FormatException)
            {
                var aside = file + ".bad-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                try { File.Move(file, aside); }
                catch { return new AppSettings { Unreadable = true }; }
                SetAsidePath = aside;
                return new AppSettings();
            }
        }

        static AppSettings FromJson(Dictionary<string, object> o)
        {
            var s = new AppSettings
            {
                Language = o.Str("Language") ?? "auto",
                AskEveryTime = o.Bool("AskEveryTime", true),
                DefaultTarget = o.Obj("DefaultTarget") is Dictionary<string, object> t ? Target.FromJson(t) : null,
                ShowProfiles = o.Bool("ShowProfiles", true),
            };
            foreach (var r in (o.Arr("Rules") ?? new List<object>()).OfType<Dictionary<string, object>>())
            {
                // Normalized, so a hand-written "WWW.GitHub.com" matches like "github.com".
                var domain = NormalizeDomain(r.Str("Domain"));
                var target = r.Obj("Target") is Dictionary<string, object> rt ? Target.FromJson(rt) : null;
                if (domain != null && target != null) s.SetRule(domain, target);
            }
            return s;
        }

        /// <summary>
        /// Load, change, save, as one step that other Browser Selector processes wait for (two pickers remembering
        /// a site at once must not lose a rule). Throws when the file can't be read or written.
        /// </summary>
        public static AppSettings Update(Action<AppSettings> change)
        {
            using (var mutex = new Mutex(false, @"Local\BrowserSelector.Settings"))
            {
                bool owned;
                try { owned = mutex.WaitOne(TimeSpan.FromSeconds(3)); }
                catch (AbandonedMutexException) { owned = true; }
                try
                {
                    var s = Load();
                    if (s.Unreadable) throw new IOException(FilePath + " is in use by another program. Try again.");
                    change(s);
                    s.Save();
                    return s;
                }
                finally
                {
                    if (owned) mutex.ReleaseMutex();
                }
            }
        }

        void Save()
        {
            var file = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var json = new Dictionary<string, object>
            {
                ["Language"] = Language,
                ["AskEveryTime"] = AskEveryTime,
                ["DefaultTarget"] = DefaultTarget?.ToJson(),
                ["ShowProfiles"] = ShowProfiles,
                ["Rules"] = Rules.Select(r => new Dictionary<string, object> { ["Domain"] = r.Domain, ["Target"] = r.Target.ToJson() }).ToList(),
            };
            try
            {
                File.WriteAllText(tmp, Json.Write(json) + "\n");
                if (File.Exists(file)) File.Replace(tmp, file, null, ignoreMetadataErrors: true);
                else File.Move(tmp, file);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        /// <summary>The most specific rule for a host ("docs.github.com" prefers a "docs.github.com" rule over "github.com").</summary>
        public SiteRule FindRule(string host)
        {
            if (string.IsNullOrEmpty(host)) return null;
            return Rules
                .Where(r => host == r.Domain || host.EndsWith("." + r.Domain, StringComparison.Ordinal))
                .OrderByDescending(r => r.Domain.Length)
                .FirstOrDefault();
        }

        public void SetRule(string domain, Target target)
        {
            Rules.RemoveAll(r => r.Domain == domain);
            Rules.Add(new SiteRule { Domain = domain, Target = target });
            Rules.Sort((a, b) => string.CompareOrdinal(a.Domain, b.Domain));
        }

        /// <summary>Turns "https://www.Example.com/page" or "example.com" into "example.com"; null if it isn't a domain.</summary>
        public static string NormalizeDomain(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim();
            if (!input.Contains("://")) input = "https://" + input;
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Host)) return null;
            var host = CleanHost(uri.IdnHost);
            return host.Length > 0 ? host : null;
        }

        /// <summary>
        /// Lower case, no trailing dot, and no leading "www." unless that would leave a bare top-level domain
        /// ("www.com" stays "www.com", so a rule for it can't swallow every .com site).
        /// </summary>
        public static string CleanHost(string host)
        {
            host = host.ToLowerInvariant().TrimEnd('.');
            if (host.StartsWith("www.", StringComparison.Ordinal) && host.IndexOf('.', 4) > 4) host = host.Substring(4);
            return host;
        }
    }
}
