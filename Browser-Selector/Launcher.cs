using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace BrowserSelector
{
    /// <summary>One entry in the picker: a browser, optionally a profile, optionally a private window.</summary>
    public sealed class BrowserOption
    {
        public Browser Browser { get; set; }
        public BrowserProfile Profile { get; set; }
        public bool Private { get; set; }

        public string Label => Profile == null ? Browser.Name : $"{Browser.Name} · {Profile.Name}";

        public Target ToTarget() => new Target { Browser = Browser.Id, Profile = Profile?.Id, Private = Private };

        public bool Matches(Target t) =>
            t != null && t.Browser == Browser.Id && (t.Profile ?? "") == (Profile?.Id ?? "");

        /// <summary>The picker list: one entry per profile when a browser has several, otherwise one per browser.</summary>
        public static List<BrowserOption> Build(IEnumerable<Browser> browsers, bool showProfiles)
        {
            var list = new List<BrowserOption>();
            foreach (var b in browsers)
            {
                if (showProfiles && b.Profiles.Count > 1)
                    list.AddRange(b.Profiles.Select(p => new BrowserOption { Browser = b, Profile = p }));
                else
                    list.Add(new BrowserOption { Browser = b });
            }
            return list;
        }

        /// <summary>
        /// Resolves a saved target against the installed browsers. Null when the browser is gone, or when the target
        /// names a profile that can't be found: profiles are usually how people keep work and personal apart, so
        /// opening some other profile instead would be worse than asking.
        /// </summary>
        public static BrowserOption Resolve(IEnumerable<Browser> browsers, Target t)
        {
            var b = t == null ? null : browsers.FirstOrDefault(x => x.Id == t.Browser);
            if (b == null) return null;
            BrowserProfile p = null;
            if (!string.IsNullOrEmpty(t.Profile))
            {
                // Firefox profiles were saved by name before 0.1.0-beta.3; they are saved by folder now.
                p = b.Profiles.FirstOrDefault(x => x.Id == t.Profile)
                    ?? (b.Family == BrowserFamily.Firefox ? b.Profiles.FirstOrDefault(x => x.Name == t.Profile) : null);
                if (p == null) return null;
            }
            return new BrowserOption { Browser = b, Profile = p, Private = t.Private };
        }
    }

    public static class Launcher
    {
        /// <summary>
        /// Accepts only what a browser should get: an http or https link, or a local file (a path or a file: URI).
        /// Everything else is refused: javascript:, data:, chrome:, other apps' schemes, network (UNC) files, and
        /// anything that could reach the browser as a command-line switch.
        /// </summary>
        public static bool TryNormalize(string input, out string link)
        {
            link = null;
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            if (input.Any(char.IsControl)) return false;
            bool http = input.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
                        || input.StartsWith("https:", StringComparison.OrdinalIgnoreCase);
            if (!http && !input.StartsWith(@"\\", StringComparison.Ordinal))
            {
                try
                {
                    if (Path.IsPathRooted(input) && File.Exists(input)) { link = Path.GetFullPath(input); return true; }
                }
                catch { /* not a path */ }
            }
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
            {
                bool ok = uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps
                          || (uri.IsFile && !uri.IsUnc && File.Exists(uri.LocalPath));
                if (ok) link = input;
                return ok;
            }
            // Links .NET can't parse but browsers repair or search ("http:example.com", a space in the host):
            // pass them on. They have no host, so no rule matches them.
            if (http) { link = input; return true; }
            return false;
        }

        /// <summary>The host of a web link, cleaned like rule domains (see AppSettings.CleanHost); null for files.</summary>
        public static string HostOf(string link)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
            var host = AppSettings.CleanHost(uri.IdnHost);
            return host.Length > 0 ? host : null;
        }

        public enum RouteReason { Rule, RuleUnavailable, MainBrowser, MainUnavailable, Ask }

        /// <summary>
        /// What a click on the link should open without asking: the browser of a matching site rule, else the
        /// main browser when the user chose not to be asked. Null means show the picker, which is also the answer
        /// whenever a rule or the main browser can't be followed exactly as saved (browser or profile missing).
        /// </summary>
        public static BrowserOption Route(string link, IList<Browser> browsers, AppSettings settings,
            out SiteRule rule, out RouteReason reason)
        {
            rule = settings.FindRule(HostOf(link));
            if (rule != null)
            {
                var option = BrowserOption.Resolve(browsers, rule.Target);
                reason = option != null ? RouteReason.Rule : RouteReason.RuleUnavailable;
                return option;
            }

            if (!settings.AskEveryTime && browsers.Count > 0)
            {
                // No main browser saved yet: the first one is what the settings window shows.
                var main = settings.DefaultTarget == null
                    ? new BrowserOption { Browser = browsers[0] }
                    : BrowserOption.Resolve(browsers, settings.DefaultTarget);
                reason = main != null ? RouteReason.MainBrowser : RouteReason.MainUnavailable;
                return main;
            }
            reason = RouteReason.Ask;
            return null;
        }

        public static string BuildArguments(BrowserOption o, string link)
        {
            var args = new List<string>();
            if (!string.IsNullOrEmpty(o.Browser.ExtraArgs)) args.Add(o.Browser.ExtraArgs);
            if (o.Profile != null)
            {
                if (o.Browser.Family == BrowserFamily.Chromium) args.Add(Quote("--profile-directory=" + o.Profile.Id));
                else if (o.Browser.Family == BrowserFamily.Firefox)
                    args.Add(o.Profile.Folder != null ? "-profile " + Quote(o.Profile.Folder) : "-P " + Quote(o.Profile.Name));
            }
            if (o.Private && !string.IsNullOrEmpty(o.Browser.PrivateArg)) args.Add(o.Browser.PrivateArg);
            if (link != null) args.Add(Quote(link.Replace("\"", "%22")));
            return string.Join(" ", args);
        }

        public static void Open(BrowserOption o, string link)
        {
            Process.Start(new ProcessStartInfo(o.Browser.ExePath, BuildArguments(o, link))
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(o.Browser.ExePath),
            });
        }

        /// <summary>Quotes one argument by the Windows rules (backslashes before a quote are doubled).</summary>
        static string Quote(string s)
        {
            var sb = new StringBuilder("\"");
            int slashes = 0;
            foreach (var c in s)
            {
                if (c == '\\') { slashes++; continue; }
                sb.Append('\\', c == '"' ? slashes * 2 + 1 : slashes);
                slashes = 0;
                sb.Append(c);
            }
            return sb.Append('\\', slashes * 2).Append('"').ToString();
        }
    }
}
