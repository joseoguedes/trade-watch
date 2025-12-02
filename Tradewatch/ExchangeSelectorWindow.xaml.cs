using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Tradewatch
{
    public partial class ExchangeSelectorWindow : Window
    {
        private List<Exchange> _exchanges;

        public ExchangeSelectorWindow(List<Exchange> exchanges)
        {
            InitializeComponent();
            _exchanges = exchanges;

            LoadCheckboxes();
        }

        private void LoadCheckboxes()
        {
            foreach (var exchange in _exchanges)
            {
                var checkbox = new CheckBox
                {
                    Content = exchange.Name,
                    IsChecked = exchange.IsEnabled,
                    Margin = new Thickness(0, 5, 0, 5),
                    Foreground = System.Windows.Media.Brushes.White,
                };

                checkbox.Tag = exchange; // store reference
                ExchangeList.Children.Add(checkbox);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = new AppSettings();
            foreach (var child in ExchangeList.Children)
            {
                if (child is CheckBox cb && cb.Tag is Exchange ex)
                {
                    ex.IsEnabled = cb.IsChecked == true;
                    if (ex.IsEnabled)
                        settings.EnabledExchanges.Add(ex.Name);
                }
            }

            // save JSON
            MainWindow main = Owner as MainWindow;
            main.SaveSettings(settings);

            DialogResult = true;
            Close();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in ExchangeList.Children)
            {
                if (child is CheckBox cb)
                {
                    cb.IsChecked = true;
                }
            }
        }
        
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in ExchangeList.Children)
            {
                if (child is CheckBox cb)
                {
                    cb.IsChecked = false;
                }
            }
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
