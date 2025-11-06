using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.Windows;

namespace Tradewatch
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize timer for clock updates every second
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateTime();
            _timer.Start();

            // Set up exchanges
            var exchanges = GetExchanges();
            ExchangeGrid.ItemsSource = exchanges;

            // Initial time update
            UpdateTime();
        }

        private void UpdateTime()
        {
            LocalTimeText.Text = DateTime.Now.ToString("HH:mm:ss");

            var nowUtc = DateTime.UtcNow;
            var updatedList = GetExchanges().Select(e =>
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(e.TimeZone);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);

                bool isOpen = localTime.TimeOfDay >= e.Open && localTime.TimeOfDay <= e.Close;
                return new Exchange
                {
                    Name = e.Name,
                    TimeZone = e.TimeZone,
                    Open = e.Open,
                    Close = e.Close,
                    LocalTime = localTime.ToString("HH:mm"),
                    OpenCloseHours = $"{e.Open:hh\\:mm} - {e.Close:hh\\:mm}",
                    Status = isOpen ? "🟢 Open" : "🔴 Closed"
                };
            }).ToList();

            ExchangeGrid.ItemsSource = updatedList;
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
    }

    public class Exchange
    {
        public string Name { get; set; }
        public string TimeZone { get; set; }
        public TimeSpan Open { get; set; }
        public TimeSpan Close { get; set; }

        // Display properties
        public string LocalTime { get; set; }
        public string OpenCloseHours { get; set; }
        public string Status { get; set; }
    }
}