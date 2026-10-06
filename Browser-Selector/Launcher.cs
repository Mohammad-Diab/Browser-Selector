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

        /// <summary>Resolves a saved target against the installed browsers; null when the browser is gone.</summary>
        public static BrowserOption Resolve(IEnumerable<Browser> browsers, Target t)
        {
            var b = t == null ? null : browsers.FirstOrDefault(x => x.Id == t.Browser);
            if (b == null) return null;
            var p = string.IsNullOrEmpty(t.Profile) ? null : b.Profiles.FirstOrDefault(x => x.Id == t.Profile);
            return new BrowserOption { Browser = b, Profile = p, Private = t.Private };
        }
    }

    public static class Launcher
    {
        /// <summary>
        /// Accepts only what a browser should get: an absolute URL with a scheme, or an existing local file.
        /// This also keeps a "link" like "--some-switch" from reaching the browser as a command-line switch.
        /// </summary>
        public static bool TryNormalize(string input, out string link)
        {
            link = null;
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            try
            {
                if (Path.IsPathRooted(input) && File.Exists(input)) { link = Path.GetFullPath(input); return true; }
            }
            catch { /* not a path */ }
            if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && char.IsLetter(input[0])
                && (!uri.IsFile || File.Exists(uri.LocalPath)))
            {
                link = input;
                return true;
            }
            return false;
        }

        /// <summary>The host of a web link without "www.", or null for files and other schemes.</summary>
        public static string HostOf(string link)
        {
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) return null;
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return null;
            var host = uri.IdnHost.ToLowerInvariant();
            return host.StartsWith("www.") ? host.Substring(4) : host;
        }

        public enum RouteReason { Rule, MainBrowser, Ask }

        /// <summary>
        /// What a click on the link should open without asking: the browser of a matching site rule, else the
        /// main browser when the user chose not to be asked. Null means show the picker. <paramref name="rule"/>
        /// is the matching rule even when its browser is gone.
        /// </summary>
        public static BrowserOption Route(string link, IList<Browser> browsers, AppSettings settings,
            out SiteRule rule, out RouteReason reason)
        {
            rule = settings.FindRule(HostOf(link));
            var option = rule == null ? null : BrowserOption.Resolve(browsers, rule.Target);
            if (option != null) { reason = RouteReason.Rule; return option; }

            if (!settings.AskEveryTime && browsers.Count > 0)
            {
                reason = RouteReason.MainBrowser;
                return BrowserOption.Resolve(browsers, settings.DefaultTarget) ?? new BrowserOption { Browser = browsers[0] };
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
                else if (o.Browser.Family == BrowserFamily.Firefox) args.Add("-P " + Quote(o.Profile.Id));
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
