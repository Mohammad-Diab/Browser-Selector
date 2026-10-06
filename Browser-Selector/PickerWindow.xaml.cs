using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace BrowserSelector
{
    public sealed class PickerItem
    {
        public BrowserOption Option { get; set; }
        public string Number { get; set; }
        public bool IsDefault { get; set; }
    }

    /// <summary>The small "open with" window that pops up next to the mouse for each link.</summary>
    public partial class PickerWindow : Window
    {
        readonly string link;
        readonly string host;
        readonly AppSettings settings;
        readonly List<PickerItem> items;

        // Closes when another window comes to the foreground, like a context menu (see OnForegroundChanged).
        static readonly TimeSpan FocusGrace = TimeSpan.FromMilliseconds(600);
        DateTime shownAt = DateTime.MaxValue;
        bool keepOpen;      // our own message box or settings window is taking the focus
        bool closing;
        bool stolenEarly;   // another app took the foreground during the grace period
        IntPtr hook;
        Native.WinEventProc foregroundProc; // kept in a field so the GC doesn't collect the callback

        public PickerWindow(string link, List<Browser> browsers, AppSettings settings)
        {
            InitializeComponent();
            FlowDirection = Loc.Flow;
            SizeToContent = SizeToContent.Height;
            this.link = link;
            this.settings = settings;
            host = Launcher.HostOf(link);

            HostText.Text = host ?? (File.Exists(link) ? Loc.T("Picker.File") : new Uri(link).Scheme + ":");
            LinkText.Text = link;
            LinkText.ToolTip = link;
            Title = "Browser Selector — " + (host ?? link);

            var options = BrowserOption.Build(browsers, settings.ShowProfiles);
            items = options.Select((o, i) => new PickerItem { Option = o, Number = i < 9 ? (i + 1).ToString() : "" }).ToList();
            int selected = items.FindIndex(x => x.Option.Matches(settings.DefaultTarget));
            if (selected < 0) selected = Math.Max(0, items.FindIndex(x => x.Option.Browser.Id == settings.DefaultTarget?.Browser));
            if (items.Count > 0) items[selected].IsDefault = true;
            List.ItemsSource = items;
            List.SelectedIndex = items.Count > 0 ? selected : -1;

            if (items.Count == 0)
            {
                List.Visibility = Visibility.Collapsed;
                NoBrowsers.Visibility = Visibility.Visible;
                PrivateBox.Visibility = RememberBox.Visibility = HintText.Visibility = Visibility.Collapsed;
            }
            // A rule for a host without a dot ("localhost", or "com" from www.com) would be meaningless or far too broad.
            if (host == null || !host.Contains(".")) RememberBox.Visibility = Visibility.Collapsed;
            else RememberBox.Content = new TextBlock { Text = Loc.F("Picker.Remember", host), TextTrimming = TextTrimming.CharacterEllipsis };

            TitleBar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
            SourceInitialized += (s, e) =>
            {
                Theme.StyleWindow(this, roundCorners: true);
                PlaceNearCursor();
                foregroundProc = OnForegroundChanged;
                hook = Native.SetWinEventHook(Native.EVENT_SYSTEM_FOREGROUND, Native.EVENT_SYSTEM_FOREGROUND,
                    IntPtr.Zero, foregroundProc, 0, 0, Native.WINEVENT_OUTOFCONTEXT);
            };
            Loaded += (s, e) => FocusSelected();
            ContentRendered += (s, e) =>
            {
                shownAt = DateTime.UtcNow;
                // If an app grabbed the foreground right away, try once more to get it back for the keyboard.
                var retry = new DispatcherTimer { Interval = FocusGrace };
                retry.Tick += (_, __) =>
                {
                    retry.Stop();
                    if (stolenEarly && !closing && Native.GetForegroundWindow() != Handle) FocusSelected();
                };
                retry.Start();
            };
        }

        IntPtr Handle => new WindowInteropHelper(this).Handle;

        /// <summary>
        /// Another window came to the foreground: the user moved on, so close. Watching the foreground (not just
        /// our own deactivation) also covers the picker being left inactive. Exceptions: our own message boxes,
        /// and the first moments after the picker appears, when apps like Outlook or Teams take the foreground
        /// back after opening a link; the picker stays on top then and closes on the next change.
        /// </summary>
        void OnForegroundChanged(IntPtr hook, uint evt, IntPtr hwnd, int idObject, int idChild, uint thread, uint time)
        {
            var me = Handle;
            if (closing || keepOpen || hwnd == me || Native.GetWindow(hwnd, Native.GW_OWNER) == me) return;
            if (DateTime.UtcNow - shownAt < FocusGrace) { stolenEarly = true; return; }
            Dispatcher.BeginInvoke(new Action(() => { if (!closing) Close(); }));
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            closing = true;
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (hook != IntPtr.Zero) Native.UnhookWinEvent(hook);
            base.OnClosed(e);
        }

        /// <summary>Puts the window next to the mouse, inside the work area of the monitor under it.</summary>
        void PlaceNearCursor()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (!Native.GetCursorPos(out var pt)) return;
            var monitor = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST);
            var info = new Native.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO)) };
            if (!Native.GetMonitorInfo(monitor, ref info)) return;
            double scale = 1;
            try { if (Native.GetDpiForMonitor(monitor, 0, out var dpi, out _) == 0) scale = dpi / 96.0; } catch { }

            ((UIElement)Content).Measure(new Size(Width, double.PositiveInfinity));
            int w = (int)Math.Ceiling(Width * scale);
            int h = (int)Math.Ceiling(((UIElement)Content).DesiredSize.Height * scale);
            var work = info.rcWork;
            int margin = (int)(8 * scale);
            // Like a context menu: a corner at the mouse, opening away from it, so the mouse
            // never rests on a button (the close button sits on the left in Arabic).
            int x = Loc.IsArabic ? pt.X - w : pt.X;
            int y = pt.Y;
            x = Math.Max(work.Left + margin, Math.Min(x, work.Right - w - margin));
            y = Math.Max(work.Top + margin, Math.Min(y, work.Bottom - h - margin));
            Native.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }

        void FocusSelected()
        {
            Activate();
            Native.SetForegroundWindow(new WindowInteropHelper(this).Handle);
            if (List.SelectedIndex < 0) return;
            List.UpdateLayout();
            (List.ItemContainerGenerator.ContainerFromIndex(List.SelectedIndex) as ListBoxItem)?.Focus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            int index = -1;
            if (key >= Key.D1 && key <= Key.D9) index = key - Key.D1;
            else if (key >= Key.NumPad1 && key <= Key.NumPad9) index = key - Key.NumPad1;

            if (key == Key.Escape) Close();
            else if (index >= 0 && !ctrl) { if (index < items.Count) Choose(items[index], shift); }
            else if (key == Key.Enter && List.SelectedItem is PickerItem item) Choose(item, shift);
            else if (key == Key.C && ctrl) CopyLink();
            else { base.OnPreviewKeyDown(e); return; }
            e.Handled = true;
        }

        void Item_Click(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is PickerItem item)
                Choose(item, (Keyboard.Modifiers & ModifierKeys.Shift) != 0);
        }

        void Choose(PickerItem item, bool flipPrivate)
        {
            var option = new BrowserOption
            {
                Browser = item.Option.Browser,
                Profile = item.Option.Profile,
                Private = (PrivateBox.IsChecked == true) ^ flipPrivate,
            };
            try
            {
                Launcher.Open(option, link);
            }
            catch (Exception ex)
            {
                keepOpen = true; // so the user can pick another browser after reading the error
                MessageBox.Show(this, Loc.F("Error.Launch", option.Browser.Name, ex.Message), Loc.T("Error.Title"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
                keepOpen = false;
                Activate();
                return;
            }
            if (RememberBox.IsChecked == true && RememberBox.Visibility == Visibility.Visible)
            {
                try
                {
                    var target = option.ToTarget();
                    settings.Rules = AppSettings.Update(s => s.SetRule(host, target)).Rules;
                }
                catch (Exception ex)
                {
                    // The link is open, but say that the rule wasn't saved instead of losing it quietly.
                    keepOpen = true;
                    MessageBox.Show(this, Loc.F("Error.RuleNotSaved", host, ex.Message), Loc.T("Error.Title"),
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            Close();
        }

        void CopyLink()
        {
            for (int i = 0; i < 5; i++)
            {
                try { Clipboard.SetText(link); break; }
                catch { System.Threading.Thread.Sleep(40); } // the clipboard can be briefly locked by another app
            }
            HintText.Text = Loc.T("Picker.Copied");
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            timer.Tick += (s, e) => { timer.Stop(); HintText.Text = Loc.T("Picker.Hint"); };
            timer.Start();
        }

        void Copy_Click(object sender, RoutedEventArgs e) => CopyLink();

        void Settings_Click(object sender, RoutedEventArgs e)
        {
            keepOpen = true; // the settings window takes the focus; we close ourselves right after
            new SettingsWindow().Show();
            Close();
        }

        void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
