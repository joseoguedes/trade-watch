using System;
using System.ComponentModel;
using System.Windows.Media;

namespace Tradewatch
{
    public class Exchange : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string TimeZone { get; set; }
        public TimeSpan Open { get; set; }
        public TimeSpan Close { get; set; }

        // This is the new property we added
        public bool IsEnabled { get; set; } = true;

        private string _localTime;
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

        private string _openCloseHours;
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

        private string _status;
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string StatusText => Status == "Open" ? "Open" : "Closed";
        public Brush StatusColor => Status == "Open" ? Brushes.LimeGreen : Brushes.Red;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
