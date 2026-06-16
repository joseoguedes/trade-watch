using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using WinForms = System.Windows.Forms;

namespace Tradewatch
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);

        private ObservableCollection<Exchange> Exchanges { get; set; }
        private readonly DispatcherTimer _timer;
        private readonly string SettingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private bool _isDark = true;
        private ExchangeSelectorWindow _selectorWindow;
        private WinForms.NotifyIcon _trayIcon;
        private bool _isExiting;
        private System.Drawing.Icon _iconOpen;
        private System.Drawing.Icon _iconClosed;
        private readonly Dictionary<string, string> _lastKnownStatus = new();

        public MainWindow()
        {
            InitializeComponent();

            // Initialize timer for clock updates every second
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateTime();
            _timer.Start();

            // Set up exchanges
            Exchanges = new ObservableCollection<Exchange>(GetExchanges());
            var settings = LoadSettings();
            foreach (var ex in Exchanges)
            {
                ex.IsEnabled = settings.EnabledExchanges.Contains(ex.Name);
            }
            ExchangeGrid.ItemsSource = Exchanges.Where(x => x.IsEnabled).ToList();

            _iconOpen = CreateCircleIcon(System.Drawing.Color.LimeGreen);
            _iconClosed = CreateCircleIcon(System.Drawing.Color.Gray);
            InitializeTrayIcon();

            // Initial time update
            UpdateTime();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = LoadSettings();
            ApplyTheme(isDark: settings.SelectedTheme != "Light");
            Topmost = settings.AlwaysOnTop;
            AlwaysOnTopMenuItem.IsChecked = settings.AlwaysOnTop;
        }
        private void UpdateTime()
        {
            LocalTimeText.Text = DateTime.Now.ToString("HH:mm:ss");

            var nowUtc = DateTime.UtcNow;


            foreach (var e in Exchanges)
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(e.TimeZone);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
                var effectiveLunchEnd = (localTime.DayOfWeek == DayOfWeek.Friday && e.FridayLunchEnd.HasValue)
                    ? e.FridayLunchEnd
                    : e.LunchEnd;
                bool inLunch = e.LunchStart.HasValue && effectiveLunchEnd.HasValue
                            && localTime.TimeOfDay >= e.LunchStart.Value
                            && localTime.TimeOfDay < effectiveLunchEnd.Value;
                bool isOpen = localTime.TimeOfDay >= e.Open && localTime.TimeOfDay < e.Close
                            && !e.WeekendDays.Contains(localTime.DayOfWeek)
                            && !inLunch;

                e.LocalTime = localTime.ToString("HH:mm");
                e.OpenCloseHours = $"{e.Open:hh\\:mm} - {e.Close:hh\\:mm}";
                e.Status = isOpen ? "Open" : "Closed";
                e.Countdown = ComputeCountdown(e, localTime, isOpen, inLunch, effectiveLunchEnd);
            }

            bool anyOpen = Exchanges.Any(ex => ex.IsEnabled && ex.Status == "Open");
            UpdateTrayIcon(anyOpen);
            NotifyStatusChanges();
        }

        private string ComputeCountdown(Exchange e, DateTime localTime, bool isOpen, bool inLunch, TimeSpan? effectiveLunchEnd)
        {
            if (isOpen)
            {
                var remaining = e.Close - localTime.TimeOfDay;
                return "Closes in " + FormatCountdown(remaining);
            }

            if (inLunch && effectiveLunchEnd.HasValue)
            {
                var remaining = effectiveLunchEnd.Value - localTime.TimeOfDay;
                return "Opens in " + FormatCountdown(remaining);
            }

            var nextOpen = GetNextOpen(e, localTime);
            return "Opens in " + FormatCountdown(nextOpen - localTime);
        }

        private DateTime GetNextOpen(Exchange e, DateTime localTime)
        {
            var today = localTime.Date;

            if (!e.WeekendDays.Contains(today.DayOfWeek) && localTime.TimeOfDay < e.Open)
                return today + e.Open;

            for (int i = 1; i <= 7; i++)
            {
                var next = today.AddDays(i);
                if (!e.WeekendDays.Contains(next.DayOfWeek))
                    return next + e.Open;
            }

            return localTime;
        }

        private static string FormatCountdown(TimeSpan span)
        {
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            if (span.TotalMinutes >= 1)
                return $"{span.Minutes}m {span.Seconds:D2}s";
            return $"{span.Seconds}s";
        }
        private List<Exchange> GetExchanges()
        {
            return new List<Exchange>
            {
                new Exchange { Name = "New York Stock Exchange (NYSE)", TimeZone = "Eastern Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "London Stock Exchange (LSE)", TimeZone = "GMT Standard Time", Open = new TimeSpan(8,0,0), Close = new TimeSpan(16,30,0) },
                new Exchange { Name = "Tokyo Stock Exchange (TSE)", TimeZone = "Tokyo Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(15,0,0), LunchStart = new TimeSpan(11,30,0), LunchEnd = new TimeSpan(12,30,0) },
                new Exchange { Name = "Hong Kong Stock Exchange (HKEX)", TimeZone = "China Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0), LunchStart = new TimeSpan(12,0,0), LunchEnd = new TimeSpan(13,0,0) },
                new Exchange { Name = "Euronext Paris", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Australian Securities Exchange (ASX)", TimeZone = "AUS Eastern Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "NASDAQ", TimeZone = "Eastern Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "Toronto Stock Exchange (TSX)", TimeZone = "Eastern Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "Deutsche Börse (Frankfurt)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "SIX Swiss Exchange (Zurich)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Madrid Stock Exchange (Bolsa de Madrid)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Borsa Italiana (Milan)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Euronext Amsterdam", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Euronext Dublin (ISEQ)", TimeZone = "GMT Standard Time", Open = new TimeSpan(8,0,0), Close = new TimeSpan(16,30,0) },
                new Exchange { Name = "Euronext Brussels", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Vienna Stock Exchange (WBAG)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Athens Stock Exchange (ATHEX)", TimeZone = "GTB Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(17,20,0) },
                new Exchange { Name = "Oslo Stock Exchange (OSE)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(16,25,0) },
                new Exchange { Name = "Stockholm Stock Exchange (OMX)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Helsinki Stock Exchange (NASDAQ OMX)", TimeZone = "FLE Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(18,30,0) },
                new Exchange { Name = "Shanghai Stock Exchange (SSE)", TimeZone = "China Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(15,0,0), LunchStart = new TimeSpan(11,30,0), LunchEnd = new TimeSpan(13,0,0) },
                new Exchange { Name = "Shenzhen Stock Exchange (SZSE)", TimeZone = "China Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(15,0,0), LunchStart = new TimeSpan(11,30,0), LunchEnd = new TimeSpan(13,0,0) },
                new Exchange { Name = "Singapore Exchange (SGX)", TimeZone = "Singapore Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,0,0) },
                new Exchange { Name = "Bombay Stock Exchange (BSE)", TimeZone = "India Standard Time", Open = new TimeSpan(9,15,0), Close = new TimeSpan(15,30,0) },
                new Exchange { Name = "National Stock Exchange of India (NSE)", TimeZone = "India Standard Time", Open = new TimeSpan(9,15,0), Close = new TimeSpan(15,30,0) },
                new Exchange { Name = "Korea Exchange (KRX)", TimeZone = "Korea Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(15,30,0) },
                new Exchange { Name = "Taiwan Stock Exchange (TWSE)", TimeZone = "Taipei Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(13,30,0) },
                new Exchange { Name = "Indonesia Stock Exchange (IDX)", TimeZone = "SE Asia Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(16,0,0), LunchStart = new TimeSpan(11,30,0), LunchEnd = new TimeSpan(13,30,0), FridayLunchEnd = new TimeSpan(14,0,0) },
                new Exchange { Name = "Australian Securities Exchange (ASX 24 Futures)", TimeZone = "AUS Eastern Standard Time", Open = new TimeSpan(9,50,0), Close = new TimeSpan(16,30,0) },
                new Exchange { Name = "New Zealand Exchange (NZX)", TimeZone = "New Zealand Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(16,45,0) },
                new Exchange { Name = "São Paulo Stock Exchange (B3)", TimeZone = "E. South America Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(17,0,0) },
                new Exchange { Name = "Buenos Aires Stock Exchange (BCBA)", TimeZone = "Argentina Standard Time", Open = new TimeSpan(11,0,0), Close = new TimeSpan(17,0,0) },
                new Exchange { Name = "Johannesburg Stock Exchange (JSE)", TimeZone = "South Africa Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,0,0) },
                new Exchange { Name = "Saudi Stock Exchange (Tadawul)", TimeZone = "Arab Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(15,0,0), WeekendDays = new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday } },
                new Exchange { Name = "Tel Aviv Stock Exchange (TASE)", TimeZone = "Israel Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(17,30,0), WeekendDays = new HashSet<DayOfWeek> { DayOfWeek.Friday, DayOfWeek.Saturday } },
                new Exchange { Name = "Euronext Lisbon (PSI)", TimeZone = "GMT Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) }
            };
        }
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExitApp();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnClosing(e);
        }

        private void InitializeTrayIcon()
        {
            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Show", null, (s, e) => ShowWindow());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());

            _trayIcon = new WinForms.NotifyIcon
            {
                Icon = _iconClosed,
                Text = "Tradewatch",
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApp()
        {
            _isExiting = true;
            _timer.Stop();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void UpdateTrayIcon(bool anyOpen)
        {
            _trayIcon.Icon = anyOpen ? _iconOpen : _iconClosed;
            _trayIcon.Text = anyOpen ? "Tradewatch — markets open" : "Tradewatch — all markets closed";
        }

        private void NotifyStatusChanges()
        {
            var nowOpen = new List<string>();
            var nowClosed = new List<string>();

            foreach (var ex in Exchanges.Where(e => e.IsEnabled))
            {
                _lastKnownStatus.TryGetValue(ex.Name, out var previous);
                if (previous == ex.Status || previous == null)
                {
                    _lastKnownStatus[ex.Name] = ex.Status;
                    continue;
                }

                if (ex.Status == "Open") nowOpen.Add(ex.Name);
                else nowClosed.Add(ex.Name);

                _lastKnownStatus[ex.Name] = ex.Status;
            }

            if (nowOpen.Count > 0)
                _trayIcon.ShowBalloonTip(4000, "Tradewatch", $"{string.Join(", ", nowOpen)} now Open", WinForms.ToolTipIcon.Info);

            if (nowClosed.Count > 0)
                _trayIcon.ShowBalloonTip(4000, "Tradewatch", $"{string.Join(", ", nowClosed)} now Closed", WinForms.ToolTipIcon.Info);
        }

        private System.Drawing.Icon CreateCircleIcon(System.Drawing.Color color)
        {
            using var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                using var brush = new System.Drawing.SolidBrush(color);
                g.FillEllipse(brush, 1, 1, 14, 14);
            }
            IntPtr hIcon = bmp.GetHicon();
            var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);
            return icon;
        }
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Tradewatch\n\nA simple app to track global market open hours.\nCreated by Jose.",
                "About Tradewatch",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        public void RefreshGrid()
        {
            ExchangeGrid.ItemsSource = Exchanges.Where(x => x.IsEnabled).ToList();
        }

        private void ManageExchanges_Click(object sender, RoutedEventArgs e)
        {
            if (_selectorWindow != null && _selectorWindow.IsLoaded)
            {
                _selectorWindow.Activate();
                return;
            }

            _selectorWindow = new ExchangeSelectorWindow(Exchanges.ToList(), _isDark);
            _selectorWindow.Owner = this;
            _selectorWindow.Show();
        }
        // Theme toggles
        private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            Topmost = AlwaysOnTopMenuItem.IsChecked;
            var settings = LoadSettings();
            settings.AlwaysOnTop = Topmost;
            SaveSettings(settings);
        }

        private void DarkTheme_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(isDark: true);
        }
        private void LightTheme_Click(object sender, RoutedEventArgs e)
        {
            ApplyTheme(isDark: false);
        }

        // Core theme switcher
        private void ApplyTheme(bool isDark)
        {
            _isDark = isDark;

            var dicts = Application.Current.Resources.MergedDictionaries;
            dicts.Clear();
            dicts.Add(new ResourceDictionary
            {
                Source = new Uri($"Themes/{(isDark ? "Dark" : "Light")}Theme.xaml", UriKind.Relative)
            });

            Brush bg, fg, separator;
            if (isDark)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                RootGrid.Background = Brushes.Black;
                MenuBar.Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x4E, 0x4A));
                LocalTimeLabel.Foreground = Brushes.White;
                LocalTimeText.Foreground = Brushes.White;
                ExchangeGrid.Background = Brushes.Black;
                ExchangeGrid.Foreground = Brushes.White;
                ExchangeGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                ExchangeGrid.RowBackground = Brushes.Black;
                bg = new SolidColorBrush(Color.FromRgb(11, 61, 58));
                fg = Brushes.White;
                separator = new SolidColorBrush(Color.FromRgb(30, 138, 128));
            }
            else
            {
                this.Background = Brushes.WhiteSmoke;
                RootGrid.Background = Brushes.WhiteSmoke;
                MenuBar.Background = new SolidColorBrush(Color.FromRgb(0x5B, 0x8D, 0xB8));
                LocalTimeLabel.Foreground = Brushes.Black;
                LocalTimeText.Foreground = Brushes.Black;
                ExchangeGrid.Background = Brushes.White;
                ExchangeGrid.Foreground = Brushes.Black;
                ExchangeGrid.AlternatingRowBackground = Brushes.WhiteSmoke;
                ExchangeGrid.RowBackground = Brushes.White;
                bg = new SolidColorBrush(Color.FromRgb(0xA8, 0xC4, 0xDC));
                fg = Brushes.Black;
                separator = new SolidColorBrush(Color.FromRgb(0x8B, 0xAE, 0xC8));
            }

            ExchangeGrid.VerticalGridLinesBrush = separator;
            ExchangeGrid.HorizontalGridLinesBrush = separator;

            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, bg));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, fg));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, separator));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
            ExchangeGrid.ColumnHeaderStyle = headerStyle;

            var centeredHeaderStyle = new Style(typeof(DataGridColumnHeader));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, bg));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, fg));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, separator));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));

            foreach (var col in ExchangeGrid.Columns.Skip(1))
                col.HeaderStyle = centeredHeaderStyle;

            var settings = LoadSettings();
            settings.SelectedTheme = isDark ? "Dark" : "Light";
            SaveSettings(settings);
        }
        private AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return new AppSettings
                    {
                        EnabledExchanges = Exchanges.Select(e => e.Name).ToList()
                    };
                }
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings
                {
                    EnabledExchanges = Exchanges.Select(e => e.Name).ToList()
                };
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
}  
