using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrowserSelector
{
    public sealed class OptionItem
    {
        public BrowserOption Option { get; set; }
        public ImageSource Icon => Option?.Browser.Icon;
        public string Label { get; set; }
    }

    public sealed class RuleItem
    {
        public SiteRule Rule { get; set; }
        public string Domain => Rule.Domain;
        public ImageSource Icon { get; set; }
        public string Label { get; set; }
    }

    public sealed class BrowserItem
    {
        public Browser Browser { get; set; }
        public string Profiles => Browser.Profiles.Count == 0 ? ""
            : Loc.F("Browsers.Profiles", string.Join(", ", Browser.Profiles.Select(p => p.Name)));
    }

    public partial class SettingsWindow : Window
    {
        readonly AppSettings settings = AppSettings.Load();
        List<Browser> browsers;
        bool loading;

        public SettingsWindow(int tab = 0, bool showAdvanced = false)
        {
            InitializeComponent();
            FlowDirection = Loc.Flow;
            SourceInitialized += (s, e) => Theme.StyleWindow(this, roundCorners: false);
            Activated += (s, e) => { RefreshStatus(); ReloadRules(); };

            loading = true;
            LanguageCombo.ItemsSource = new[] { Loc.T("Lang.Auto"), "English", "العربية" };
            LanguageCombo.SelectedIndex = settings.Language == "en" ? 1 : settings.Language == "ar" ? 2 : 0;
            ShowProfilesBox.IsChecked = settings.ShowProfiles;
            AskRadio.IsChecked = settings.AskEveryTime;
            AutoRadio.IsChecked = !settings.AskEveryTime;
            AdvancedToggle.IsChecked = showAdvanced;
            AdvancedPanel.Visibility = showAdvanced ? Visibility.Visible : Visibility.Collapsed;
            loading = false;

            var version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            VersionText.Text = Loc.F("About.Version", version);
            SettingsPathBox.Text = AppSettings.FilePath;
            try
            {
                var decoder = BitmapDecoder.Create(new Uri("pack://application:,,,/app.ico"), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                AboutIcon.Source = decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
            }
            catch { /* cosmetic */ }

            LoadBrowsers();
            Tabs.SelectedIndex = tab;
        }

        void LoadBrowsers()
        {
            browsers = BrowserCatalog.Discover();
            BrowsersList.ItemsSource = browsers.Select(b => new BrowserItem { Browser = b }).ToList();
            BrowsersEmpty.Visibility = browsers.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            var options = BrowserOption.Build(browsers, showProfiles: true)
                .Select(o => new OptionItem { Option = o, Label = o.Label }).ToList();

            loading = true;
            // With no main browser saved yet, the first one is what both modes use, so show it.
            DefaultCombo.ItemsSource = options;
            DefaultCombo.SelectedItem = options.FirstOrDefault(x => x.Option.Matches(settings.DefaultTarget))
                ?? options.FirstOrDefault(x => x.Option.Browser.Id == settings.DefaultTarget?.Browser)
                ?? options.FirstOrDefault();
            RuleTarget.ItemsSource = options;
            RuleTarget.SelectedIndex = options.Count > 0 ? 0 : -1;
            loading = false;

            ReloadRules();
        }

        void ReloadRules()
        {
            settings.Rules = AppSettings.Load().Rules; // the picker may have added some
            RulesList.ItemsSource = settings.Rules.Select(r =>
            {
                var o = BrowserOption.Resolve(browsers, r.Target);
                var label = o == null ? $"{r.Target.Browser} ({Loc.T("Rules.Missing")})" : o.Label;
                if (o != null && o.Profile == null && !string.IsNullOrEmpty(r.Target.Profile)) label += " · " + r.Target.Profile;
                if (r.Target.Private) label += $" ({Loc.T("Rules.Private")})";
                return new RuleItem { Rule = r, Icon = o?.Browser.Icon, Label = label };
            }).ToList();
            RulesEmpty.Visibility = settings.Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        void RefreshStatus()
        {
            var status = Registration.GetStatus(BrowserCatalog.SelfPath);
            bool isDefault = status != Registration.Status.NotRegistered && Registration.IsDefault();
            StatusText.Text = Loc.T(status == Registration.Status.Registered ? "Reg.Registered"
                : status == Registration.Status.OtherPath ? "Reg.OtherPath" : "Reg.NotRegistered");
            StatusDot.Fill = (Brush)FindResource(status == Registration.Status.Registered && isDefault ? "Ok"
                : status == Registration.Status.NotRegistered ? "Muted" : "Accent");
            DefaultText.Text = status == Registration.Status.NotRegistered ? "" : Loc.T(isDefault ? "Reg.IsDefault" : "Reg.NotDefault");
            DefaultText.Visibility = DefaultText.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            UnregisterButton.IsEnabled = status != Registration.Status.NotRegistered;
        }

        void Save()
        {
            try { settings.Save(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        void Register_Click(object sender, RoutedEventArgs e)
        {
            try { Registration.Register(BrowserCatalog.SelfPath); }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.F("Reg.Failed", ex.Message), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            RefreshStatus();
            MessageBox.Show(this, Loc.T("Reg.Done"), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            OpenDefaultApps_Click(sender, e);
        }

        void Unregister_Click(object sender, RoutedEventArgs e)
        {
            try { Registration.Unregister(); }
            catch (Exception ex)
            {
                MessageBox.Show(this, Loc.F("Reg.Failed", ex.Message), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            RefreshStatus();
            MessageBox.Show(this, Loc.T("Reg.Removed"), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void OpenDefaultApps_Click(object sender, RoutedEventArgs e)
        {
            try { Registration.OpenDefaultAppsSettings(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        void DefaultCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading) return;
            settings.DefaultTarget = (DefaultCombo.SelectedItem as OptionItem)?.Option?.ToTarget();
            Save();
        }

        void OpenMode_Checked(object sender, RoutedEventArgs e)
        {
            if (loading) return;
            settings.AskEveryTime = AskRadio.IsChecked == true;
            // Save the main browser the combo shows, so "open without asking" never depends on list order.
            if (settings.DefaultTarget == null)
                settings.DefaultTarget = (DefaultCombo.SelectedItem as OptionItem)?.Option?.ToTarget();
            Save();
        }

        void EditRules_Click(object sender, RoutedEventArgs e) => Tabs.SelectedIndex = 1;

        void AdvancedToggle_Click(object sender, RoutedEventArgs e) =>
            AdvancedPanel.Visibility = AdvancedToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        void ShowProfiles_Click(object sender, RoutedEventArgs e)
        {
            settings.ShowProfiles = ShowProfilesBox.IsChecked == true;
            Save();
        }

        void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loading) return;
            settings.Language = new[] { "auto", "en", "ar" }[LanguageCombo.SelectedIndex];
            Save();
            Loc.Init(settings.Language);
            // Rebuild the window so every string and the layout direction follow the new language.
            var w = new SettingsWindow(Tabs.SelectedIndex, AdvancedToggle.IsChecked == true)
            {
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = Left, Top = Top, Width = Width, Height = Height,
            };
            w.Show();
            Close();
        }

        bool TryLink(out string link)
        {
            TryResult.Visibility = Visibility.Collapsed;
            if (Launcher.TryNormalize(TryBox.Text, out link)) return true;
            MessageBox.Show(this, Loc.F("Error.BadLink", TryBox.Text), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        void Try_Click(object sender, RoutedEventArgs e)
        {
            if (TryLink(out var link)) new PickerWindow(link, browsers, AppSettings.Load()).Show();
        }

        /// <summary>Does what clicking the link would do: a matching site rule, the main browser, or ask.</summary>
        void TryRules_Click(object sender, RoutedEventArgs e)
        {
            if (!TryLink(out var link)) return;
            var fresh = AppSettings.Load();
            var host = Launcher.HostOf(link) ?? link;
            var option = Launcher.Route(link, browsers, fresh, out var rule, out var reason);
            if (option != null)
            {
                try
                {
                    Launcher.Open(option, link);
                    var label = option.Label + (option.Private ? $" ({Loc.T("Rules.Private")})" : "");
                    ShowTryResult(reason == Launcher.RouteReason.Rule ? Loc.F("Gen.RuleMatched", rule.Domain, label)
                        : rule != null ? Loc.F("Gen.RuleMissingMain", rule.Domain, label)
                        : Loc.F("Gen.MainOpened", host, label));
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, Loc.F("Error.Launch", option.Browser.Name, ex.Message), Loc.T("Error.Title"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            ShowTryResult(rule != null ? Loc.F("Gen.RuleMissing", rule.Domain) : Loc.F("Gen.NoRule", host));
            new PickerWindow(link, browsers, fresh).Show();
        }

        void ShowTryResult(string text)
        {
            TryResult.Text = text;
            TryResult.Visibility = Visibility.Visible;
        }

        void AddRule_Click(object sender, RoutedEventArgs e)
        {
            var domain = AppSettings.NormalizeDomain(RuleDomain.Text);
            var option = (RuleTarget.SelectedItem as OptionItem)?.Option;
            RuleError.Visibility = domain == null ? Visibility.Visible : Visibility.Collapsed;
            if (domain == null || option == null) return;

            var target = option.ToTarget();
            target.Private = RulePrivate.IsChecked == true;
            settings.Rules = AppSettings.Load().Rules;
            settings.SetRule(domain, target);
            Save();
            RuleDomain.Clear();
            RulePrivate.IsChecked = false;
            ReloadRules();
        }

        void RuleDomain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { AddRule_Click(sender, e); e.Handled = true; }
        }

        void RemoveRule_Click(object sender, RoutedEventArgs e)
        {
            if (!(((FrameworkElement)sender).Tag is RuleItem item)) return;
            settings.Rules = AppSettings.Load().Rules;
            settings.Rules.RemoveAll(r => r.Domain == item.Domain);
            Save();
            ReloadRules();
        }

        void Refresh_Click(object sender, RoutedEventArgs e) => LoadBrowsers();

        void RepoLink_Click(object sender, RoutedEventArgs e)
        {
            try { Process.Start(new ProcessStartInfo("https://github.com/Mohammad-Diab/Browser-Selector") { UseShellExecute = true }); }
            catch { /* no browser to open it with */ }
        }
    }
}
