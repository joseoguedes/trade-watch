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

namespace Tradewatch
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Exchange> Exchanges { get; set; }
        private readonly DispatcherTimer _timer;
        private readonly string SettingsPath = "settings.json";

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

            // Initial time update
            UpdateTime();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme(isDark: true); // Default to dark theme
        }
        private void UpdateTime()
        {
            LocalTimeText.Text = DateTime.Now.ToString("HH:mm:ss");

            var nowUtc = DateTime.UtcNow;


            foreach (var e in Exchanges)
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(e.TimeZone);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
                bool isOpen = localTime.TimeOfDay >= e.Open && localTime.TimeOfDay <= e.Close
                            && localTime.DayOfWeek != DayOfWeek.Saturday
                            && localTime.DayOfWeek != DayOfWeek.Sunday;

                e.LocalTime = localTime.ToString("HH:mm");
                e.OpenCloseHours = $"{e.Open:hh\\:mm} - {e.Close:hh\\:mm}";
                e.Status = isOpen ? "Open" : "Closed";
            }
        }
        private List<Exchange> GetExchanges()
        {
            return new List<Exchange>
            {
                new Exchange { Name = "New York Stock Exchange (NYSE)", TimeZone = "Eastern Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "London Stock Exchange (LSE)", TimeZone = "GMT Standard Time", Open = new TimeSpan(8,0,0), Close = new TimeSpan(16,30,0) },
                new Exchange { Name = "Tokyo Stock Exchange (TSE)", TimeZone = "Tokyo Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(15,0,0) },
                new Exchange { Name = "Hong Kong Stock Exchange (HKEX)", TimeZone = "China Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "Euronext Paris", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Australian Securities Exchange (ASX)", TimeZone = "AUS Eastern Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(16,0,0) }
            };
        }
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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

        private void ManageExchanges_Click(object sender, RoutedEventArgs e)
        {
            var win = new ExchangeSelectorWindow(Exchanges.ToList());
            win.Owner = this;

            bool? result = win.ShowDialog();
            if (result == true)
            {
                var settings = LoadSettings();
                foreach (var ex in Exchanges)
                {
                    ex.IsEnabled = settings.EnabledExchanges.Contains(ex.Name);
                }
                ExchangeGrid.ItemsSource = Exchanges.Where(x => x.IsEnabled).ToList();
            }
        }
        // Theme toggles
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
            if (isDark)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(17, 17, 17)); // #111
                ExchangeGrid.Background = Brushes.Black;
                ExchangeGrid.Foreground = Brushes.White;
                ExchangeGrid.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                ExchangeGrid.RowBackground = Brushes.Black;

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(11, 61, 58))));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, Brushes.White));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                ExchangeGrid.ColumnHeaderStyle = headerStyle;
            }
            else
            {
                this.Background = Brushes.WhiteSmoke;
                ExchangeGrid.Background = Brushes.White;
                ExchangeGrid.Foreground = Brushes.Black;
                ExchangeGrid.AlternatingRowBackground = Brushes.WhiteSmoke;
                ExchangeGrid.RowBackground = Brushes.White;

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new SolidColorBrush(Color.FromRgb(200, 230, 220))));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, Brushes.Black));
                headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
                ExchangeGrid.ColumnHeaderStyle = headerStyle;
            }
        }
        private AppSettings LoadSettings()
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

        public void SaveSettings(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
}  
