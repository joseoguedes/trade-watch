using System;
using System.Globalization;
using System.Windows.Data;

namespace Tradewatch.Converters
{
    // True when the bound value equals ConverterParameter; used to tick the selected item of a menu choice.
    public class EqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Equals(System.Convert.ToString(value, CultureInfo.InvariantCulture), parameter as string);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
