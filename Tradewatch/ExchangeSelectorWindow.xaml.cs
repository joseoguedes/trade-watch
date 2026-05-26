using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tradewatch
{
    public partial class ExchangeSelectorWindow : Window
    {
        private List<Exchange> _exchanges;
        private bool _isDark;
        private bool _searchPlaceholderActive = true;

        public ExchangeSelectorWindow(List<Exchange> exchanges, bool isDark)
        {
            InitializeComponent();
            _exchanges = exchanges;
            _isDark = isDark;

            ApplyTheme();
            LoadCheckboxes();
            ShowPlaceholder();
        }

        private void ApplyTheme()
        {
            if (_isDark)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                SearchBox.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                SearchBox.CaretBrush = Brushes.White;
                SearchBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44));
                var btnBg = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
                AddAllBtn.Background = RemoveAllBtn.Background = CancelBtn.Background = btnBg;
                AddAllBtn.Foreground = RemoveAllBtn.Foreground = CancelBtn.Foreground = Brushes.White;
                SaveBtn.Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x4E, 0x4A));
                SaveBtn.Foreground = Brushes.White;
            }
            else
            {
                this.Background = Brushes.WhiteSmoke;
                SearchBox.Background = Brushes.White;
                SearchBox.CaretBrush = Brushes.Black;
                SearchBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                var btnBg = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
                AddAllBtn.Background = RemoveAllBtn.Background = CancelBtn.Background = btnBg;
                AddAllBtn.Foreground = RemoveAllBtn.Foreground = CancelBtn.Foreground = Brushes.Black;
                SaveBtn.Background = new SolidColorBrush(Color.FromRgb(0xBB, 0xBB, 0xBB));
                SaveBtn.Foreground = Brushes.Black;
            }
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
                    Foreground = _isDark ? Brushes.White : Brushes.Black,
                };

                checkbox.Tag = exchange; // store reference
                ExchangeList.Children.Add(checkbox);
            }
        }

        private void ShowPlaceholder()
        {
            SearchBox.Text = (string)SearchBox.Tag;
            SearchBox.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            _searchPlaceholderActive = true;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_searchPlaceholderActive)
            {
                SearchBox.Text = "";
                SearchBox.Foreground = _isDark ? Brushes.White : Brushes.Black;
                _searchPlaceholderActive = false;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
                ShowPlaceholder();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_searchPlaceholderActive) return;

            var query = SearchBox.Text.Trim();

            foreach (var child in ExchangeList.Children)
            {
                if (child is CheckBox cb)
                {
                    var name = cb.Content?.ToString() ?? "";
                    cb.Visibility = name.Contains(query, System.StringComparison.OrdinalIgnoreCase)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
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

            MainWindow main = Owner as MainWindow;
            main.SaveSettings(settings);
            main.RefreshGrid();
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
