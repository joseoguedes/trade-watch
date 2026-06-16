using System.Collections.Generic;

namespace Tradewatch
{
    public class AppSettings
    {
        public List<string> EnabledExchanges { get; set; } = new List<string>();
        public string SelectedTheme { get; set; } = "Dark";
        public bool AlwaysOnTop { get; set; } = false;
    }
}
