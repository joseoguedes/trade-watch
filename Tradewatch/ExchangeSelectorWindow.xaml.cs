using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Tradewatch.ViewModels;

namespace Tradewatch
{
    public partial class ExchangeSelectorWindow : Window
    {
        public ExchangeSelectorWindow(ObservableCollection<Exchange> allExchanges, bool isDark, Action onSave)
        {
            InitializeComponent();

            var vm = new ExchangeSelectorViewModel(allExchanges, onSave);
            vm.CloseRequested += Close;
            DataContext = vm;

            ApplyTheme(isDark);
        }

        public void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                this.Foreground = Brushes.White;
                SearchBox.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
                SearchBox.Foreground = Brushes.White;
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
                this.Foreground = Brushes.Black;
                SearchBox.Background = Brushes.White;
                SearchBox.Foreground = Brushes.Black;
                SearchBox.CaretBrush = Brushes.Black;
                SearchBox.BorderBrush = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
                var btnBg = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
                AddAllBtn.Background = RemoveAllBtn.Background = CancelBtn.Background = btnBg;
                AddAllBtn.Foreground = RemoveAllBtn.Foreground = CancelBtn.Foreground = Brushes.Black;
                SaveBtn.Background = new SolidColorBrush(Color.FromRgb(0xBB, 0xBB, 0xBB));
                SaveBtn.Foreground = Brushes.Black;
            }
        }

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
                Close();
            base.OnKeyDown(e);
        }
    }
}
