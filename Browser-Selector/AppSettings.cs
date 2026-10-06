using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace BrowserSelector
{
    /// <summary>What to open a link with, as saved in settings.</summary>
    public sealed class Target
    {
        public string Browser { get; set; }   // Browser.Id
        public string Profile { get; set; }   // BrowserProfile.Id, or null
        public bool Private { get; set; }
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
        public Target DefaultTarget { get; set; }
        public bool ShowProfiles { get; set; } = true;
        public List<SiteRule> Rules { get; set; } = new List<SiteRule>();

        const string FileName = "settings.json";

        /// <summary>
        /// %APPDATA%\BrowserSelector, or the app folder when a settings.json sits next to the exe (portable use).
        /// </summary>
        public static string FilePath
        {
            get
            {
                var local = Path.Combine(Path.GetDirectoryName(BrowserCatalog.SelfPath), FileName);
                if (File.Exists(local)) return local;
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserSelector", FileName);
            }
        }

        public static AppSettings Load()
        {
            try
            {
                var file = FilePath;
                if (File.Exists(file))
                {
                    var s = new JavaScriptSerializer().Deserialize<AppSettings>(File.ReadAllText(file));
                    if (s != null)
                    {
                        s.Rules = (s.Rules ?? new List<SiteRule>()).Where(r => r?.Target != null && !string.IsNullOrEmpty(r.Domain)).ToList();
                        return s;
                    }
                }
            }
            catch { /* a broken file falls back to defaults */ }
            return new AppSettings();
        }

        public void Save()
        {
            var file = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            var tmp = file + ".tmp";
            File.WriteAllText(tmp, new JavaScriptSerializer().Serialize(this));
            if (File.Exists(file)) File.Replace(tmp, file, null);
            else File.Move(tmp, file);
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
            var host = uri.IdnHost.ToLowerInvariant().TrimEnd('.');
            if (host.StartsWith("www.")) host = host.Substring(4);
            return host.Length > 0 ? host : null;
        }
    }
}
