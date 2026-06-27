using System.Collections.Generic;

namespace Tradewatch
{
    public class AppSettings
    {
        public List<string> EnabledExchanges { get; set; } = new List<string>();
        public string SelectedTheme { get; set; } = "Dark";
        public bool AlwaysOnTop { get; set; } = false;
        public bool GroupByRegion { get; set; } = true;
        public double WindowLeft { get; set; } = -1;
        public double WindowTop { get; set; } = -1;
        public double WindowWidth { get; set; } = 700;
        public double WindowHeight { get; set; } = 500;
    }
}
