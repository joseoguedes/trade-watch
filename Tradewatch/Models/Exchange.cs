using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Tradewatch
{
    public class Exchange : INotifyPropertyChanged
    {
        public string Name { get; set; } = null!;
        public string TimeZone { get; set; } = null!;
        public TimeSpan Open { get; set; }
        public TimeSpan Close { get; set; }
        public TimeSpan? LunchStart { get; set; }
        public TimeSpan? LunchEnd { get; set; }
        public TimeSpan? FridayLunchEnd { get; set; }
        public TimeSpan? PreMarketOpen { get; set; }
        public TimeSpan? PreMarketClose { get; set; }
        public TimeSpan? AfterHoursOpen { get; set; }
        public TimeSpan? AfterHoursClose { get; set; }
        public string Region { get; set; } = "";

        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned == value) return;
                _isPinned = value;
                OnPropertyChanged(nameof(IsPinned));
            }
        }
        public bool IsEnabled { get; set; } = true;
        public HashSet<DayOfWeek> WeekendDays { get; set; } = new HashSet<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };

        private string _localTime = "";
        public string LocalTime
        {
            get => _localTime;
            set
            {
                if (_localTime != value)
                {
                    _localTime = value;
                    OnPropertyChanged(nameof(LocalTime));
                }
            }
        }

        private string _openCloseHours = "";
        public string OpenCloseHours
        {
            get => _openCloseHours;
            set
            {
                if (_openCloseHours != value)
                {
                    _openCloseHours = value;
                    OnPropertyChanged(nameof(OpenCloseHours));
                }
            }
        }

        private MarketStatus _status = MarketStatus.Closed;
        public MarketStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public string StatusText => Status switch
        {
            MarketStatus.PreMarket  => "Pre-Market",
            MarketStatus.AfterHours => "After-Hours",
            _ => Status.ToString()
        };

        private string _countdown = "";
        public string Countdown
        {
            get => _countdown;
            set
            {
                if (_countdown != value)
                {
                    _countdown = value;
                    OnPropertyChanged(nameof(Countdown));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
