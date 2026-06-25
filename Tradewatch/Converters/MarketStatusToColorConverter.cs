using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Tradewatch.Converters
{
    public class MarketStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MarketStatus status)
                return status == MarketStatus.Open ? Brushes.LimeGreen
                     : status == MarketStatus.Holiday ? Brushes.Orange
                     : (object)Brushes.Gray;
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
