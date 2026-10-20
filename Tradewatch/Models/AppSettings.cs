using System.Collections.Generic;

namespace Tradewatch
{
    public class AppSettings
    {
        public List<string> EnabledExchanges { get; set; } = new List<string>();
        public string SelectedTheme { get; set; } = "Dark";
        public bool AlwaysOnTop { get; set; } = false;
        public bool GroupByRegion { get; set; } = true;
        public bool IsCompactMode { get; set; } = false;
        public bool ShowTimeline { get; set; } = false;
        public string NotifyMode { get; set; } = "All";   // "All", "Starred" or "Off"
        public int HeadsUpMinutes { get; set; } = 0;      // 0 = no heads-up before an open
        public List<string> PinnedExchanges { get; set; } = new List<string>();
        public double WindowLeft { get; set; } = -1;
        public double WindowTop { get; set; } = -1;
        public double WindowWidth { get; set; } = 700;
        public double WindowHeight { get; set; } = 500;
    }
}
