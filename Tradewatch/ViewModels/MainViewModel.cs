using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Threading;
using Tradewatch;

namespace Tradewatch.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private readonly DispatcherTimer _timer;
        private readonly Dictionary<string, string> _lastKnownStatus = new();

        public ObservableCollection<Exchange> AllExchanges { get; }

        private ObservableCollection<Exchange> _displayedExchanges;
        public ObservableCollection<Exchange> DisplayedExchanges
        {
            get => _displayedExchanges;
            private set { _displayedExchanges = value; OnPropertyChanged(); }
        }

        private string _localTime;
        public string LocalTime
        {
            get => _localTime;
            private set { _localTime = value; OnPropertyChanged(); }
        }

        private bool _isDark = true;
        public bool IsDark
        {
            get => _isDark;
            set
            {
                if (_isDark == value) return;
                _isDark = value;
                OnPropertyChanged();
                var s = LoadSettings();
                s.SelectedTheme = _isDark ? "Dark" : "Light";
                SaveSettings(s);
                ThemeChanged?.Invoke(_isDark);
            }
        }

        private bool _alwaysOnTop;
        public bool AlwaysOnTop
        {
            get => _alwaysOnTop;
            set
            {
                if (_alwaysOnTop == value) return;
                _alwaysOnTop = value;
                OnPropertyChanged();
                var s = LoadSettings();
                s.AlwaysOnTop = _alwaysOnTop;
                SaveSettings(s);
            }
        }

        // Events for code-behind to handle UI-only concerns
        public event Action<bool> ThemeChanged;
        public event Action<bool> AnyOpenChanged;
        public event Action<IReadOnlyList<string>, IReadOnlyList<string>> StatusChanged;
        public event Action ExitRequested;
        public event Action ManageExchangesRequested;
        public event Action AboutRequested;

        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand DarkThemeCommand { get; }
        public ICommand LightThemeCommand { get; }
        public ICommand ManageExchangesCommand { get; }

        public MainViewModel()
        {
            AllExchanges = new ObservableCollection<Exchange>(GetExchanges());

            var settings = LoadSettings();
            foreach (var ex in AllExchanges)
                ex.IsEnabled = settings.EnabledExchanges.Contains(ex.Name);

            _isDark = settings.SelectedTheme != "Light";
            _alwaysOnTop = settings.AlwaysOnTop;

            RefreshDisplayed();

            ExitCommand = new RelayCommand(() => ExitRequested?.Invoke());
            AboutCommand = new RelayCommand(() => AboutRequested?.Invoke());
            DarkThemeCommand = new RelayCommand(() => IsDark = true);
            LightThemeCommand = new RelayCommand(() => IsDark = false);
            ManageExchangesCommand = new RelayCommand(() => ManageExchangesRequested?.Invoke());

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => Tick();
            _timer.Start();
            Tick();
        }

        public void RefreshDisplayed()
        {
            DisplayedExchanges = new ObservableCollection<Exchange>(AllExchanges.Where(x => x.IsEnabled));
        }

        public void SaveEnabledExchanges()
        {
            var settings = LoadSettings();
            settings.EnabledExchanges = AllExchanges.Where(e => e.IsEnabled).Select(e => e.Name).ToList();
            SaveSettings(settings);
        }

        public void StopTimer() => _timer.Stop();

        private void Tick()
        {
            LocalTime = DateTime.Now.ToString("HH:mm:ss");
            var nowUtc = DateTime.UtcNow;

            foreach (var e in AllExchanges)
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(e.TimeZone);
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tz);
                var effectiveLunchEnd = (localTime.DayOfWeek == DayOfWeek.Friday && e.FridayLunchEnd.HasValue)
                    ? e.FridayLunchEnd : e.LunchEnd;
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

            bool anyOpen = AllExchanges.Any(ex => ex.IsEnabled && ex.Status == "Open");
            AnyOpenChanged?.Invoke(anyOpen);
            NotifyStatusChanges();
        }

        private void NotifyStatusChanges()
        {
            var nowOpen = new List<string>();
            var nowClosed = new List<string>();

            foreach (var ex in AllExchanges.Where(e => e.IsEnabled))
            {
                _lastKnownStatus.TryGetValue(ex.Name, out var previous);
                if (previous == null || previous == ex.Status)
                {
                    _lastKnownStatus[ex.Name] = ex.Status;
                    continue;
                }

                if (ex.Status == "Open") nowOpen.Add(ex.Name);
                else nowClosed.Add(ex.Name);

                _lastKnownStatus[ex.Name] = ex.Status;
            }

            if (nowOpen.Count > 0 || nowClosed.Count > 0)
                StatusChanged?.Invoke(nowOpen, nowClosed);
        }

        private static string ComputeCountdown(Exchange e, DateTime localTime, bool isOpen, bool inLunch, TimeSpan? effectiveLunchEnd)
        {
            if (isOpen)
                return "Closes in " + FormatCountdown(e.Close - localTime.TimeOfDay);

            if (inLunch && effectiveLunchEnd.HasValue)
                return "Opens in " + FormatCountdown(effectiveLunchEnd.Value - localTime.TimeOfDay);

            return "Opens in " + FormatCountdown(GetNextOpen(e, localTime) - localTime);
        }

        private static DateTime GetNextOpen(Exchange e, DateTime localTime)
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
            if (span < TimeSpan.Zero) span = TimeSpan.Zero;
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            if (span.TotalMinutes >= 1)
                return $"{span.Minutes}m {span.Seconds:D2}s";
            return $"{span.Seconds}s";
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return new AppSettings { EnabledExchanges = AllExchanges.Select(e => e.Name).ToList() };
                string json = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings { EnabledExchanges = AllExchanges.Select(e => e.Name).ToList() };
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }

        private static List<Exchange> GetExchanges() => new List<Exchange>
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
            new Exchange { Name = "Euronext Lisbon (PSI)", TimeZone = "GMT Standard Time", Open = new TimeSpan(9,0,0), Close = new TimeSpan(17,30,0) },
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
