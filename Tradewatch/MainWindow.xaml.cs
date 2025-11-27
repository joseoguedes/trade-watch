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
                new Exchange { Name = "Australian Securities Exchange (ASX)", TimeZone = "AUS Eastern Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "NASDAQ", TimeZone = "Eastern Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "Toronto Stock Exchange (TSX)", TimeZone = "Eastern Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "Deutsche Börse (Frankfurt)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "SIX Swiss Exchange (Zurich)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Madrid Stock Exchange (Bolsa de Madrid)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Borsa Italiana (Milan)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Euronext Amsterdam", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,40,0) },
                new Exchange { Name = "Euronext Dublin (ISEQ)", TimeZone = "GMT Standard Time", Open = new TimeSpan(8,0,0), Close = new TimeSpan(16,30,0) },
                new Exchange { Name = "Euronext Brussels", TimeZone = "Romance Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Vienna Stock Exchange (WBAG)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Athens Stock Exchange (ATHEX)", TimeZone = "GTB Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(17,20,0) },
                new Exchange { Name = "Oslo Stock Exchange (OSE)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(16,25,0) },
                new Exchange { Name = "Stockholm Stock Exchange (OMX)", TimeZone = "W. Europe Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Helsinki Stock Exchange (NASDAQ OMX)", TimeZone = "FLE Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(18,30,0) },
                new Exchange { Name = "Shanghai Stock Exchange (SSE)", TimeZone = "China Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(15,0,0) },
                new Exchange { Name = "Shenzhen Stock Exchange (SZSE)", TimeZone = "China Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(15,0,0) },
                new Exchange { Name = "Singapore Exchange (SGX)", TimeZone = "Singapore Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,0,0) },
                new Exchange { Name = "Bombay Stock Exchange (BSE)", TimeZone = "India Standard Time", Open = new TimeSpan(9,15,0), Close = new TimeSpan(15,30,0) },
                new Exchange { Name = "National Stock Exchange of India (NSE)", TimeZone = "India Standard Time", Open = new TimeSpan(9,15,0), Close = new TimeSpan(15,30,0) },
                new Exchange { Name = "Korea Exchange (KRX)", TimeZone = "Korea Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(15,30,0) },
                new Exchange { Name = "Taiwan Stock Exchange (TWSE)", TimeZone = "Taipei Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(13,30,0) },
                new Exchange { Name = "Indonesia Stock Exchange (IDX)", TimeZone = "SE Asia Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(16,0,0) },
                new Exchange { Name = "Australian Securities Exchange (ASX 24 Futures)", TimeZone = "AUS Eastern Standard Time", Open = new TimeSpan(9,50,0), Close = new TimeSpan(16,30,0) },
                new Exchange { Name = "New Zealand Exchange (NZX)", TimeZone = "New Zealand Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(16,45,0) },
                new Exchange { Name = "São Paulo Stock Exchange (B3)", TimeZone = "E. South America Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Buenos Aires Stock Exchange (BCBA)", TimeZone = "Argentina Standard Time", Open = new TimeSpan(11,0,0), Close = new TimeSpan(17,0,0) },
                new Exchange { Name = "Johannesburg Stock Exchange (JSE)", TimeZone = "South Africa Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,0,0) },
                new Exchange { Name = "Saudi Stock Exchange (Tadawul)", TimeZone = "Arab Standard Time", Open = new TimeSpan(10,0,0), Close = new TimeSpan(15,0,0) },
                new Exchange { Name = "Tel Aviv Stock Exchange (TASE)", TimeZone = "Israel Standard Time", Open = new TimeSpan(9,30,0), Close = new TimeSpan(17,30,0) },
                new Exchange { Name = "Euronext Lisbon (PSI)", TimeZone = "GMT Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) }
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
