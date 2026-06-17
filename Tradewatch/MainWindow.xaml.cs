using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Tradewatch.ViewModels;
using WinForms = System.Windows.Forms;

namespace Tradewatch
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr handle);

        private MainViewModel _vm;
        private ExchangeSelectorWindow _selectorWindow;
        private WinForms.NotifyIcon _trayIcon;
        private bool _isExiting;
        private System.Drawing.Icon _iconOpen;
        private System.Drawing.Icon _iconClosed;

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainViewModel();
            DataContext = _vm;

            var s = _vm.LoadSettings();
            if (s.WindowLeft >= 0) { Left = s.WindowLeft; Top = s.WindowTop; }
            if (s.WindowWidth > 0) Width = s.WindowWidth;
            if (s.WindowHeight > 0) Height = s.WindowHeight;

            _vm.ThemeChanged += ApplyTheme;
            _vm.AnyOpenChanged += UpdateTrayIcon;
            _vm.StatusChanged += ShowStatusBalloon;
            _vm.ExitRequested += ExitApp;
            _vm.ManageExchangesRequested += OpenExchangeSelector;
            _vm.AboutRequested += ShowAbout;

            _iconOpen = CreateCircleIcon(System.Drawing.Color.LimeGreen);
            _iconClosed = CreateCircleIcon(System.Drawing.Color.Gray);
            InitializeTrayIcon();
            UpdateTrayIcon(_vm.AllExchanges.Any(ex => ex.IsEnabled && ex.Status == "Open"));

            ApplyTheme(_vm.IsDark);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            var s = _vm.LoadSettings();
            s.WindowLeft = Left;
            s.WindowTop = Top;
            s.WindowWidth = Width;
            s.WindowHeight = Height;
            _vm.SaveSettings(s);

            if (!_isExiting)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnClosing(e);
        }

        private void InitializeTrayIcon()
        {
            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Show", null, (s, e) => ShowWindow());
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());

            _trayIcon = new WinForms.NotifyIcon
            {
                Icon = _iconClosed,
                Text = "Tradewatch",
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void ExitApp()
        {
            _isExiting = true;
            _vm.StopTimer();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Current.Shutdown();
        }

        private void UpdateTrayIcon(bool anyOpen)
        {
            _trayIcon.Icon = anyOpen ? _iconOpen : _iconClosed;
            _trayIcon.Text = anyOpen ? "Tradewatch — markets open" : "Tradewatch — all markets closed";
        }

        private void ShowStatusBalloon(IReadOnlyList<string> opened, IReadOnlyList<string> closed)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (opened.Count > 0) parts.Add($"{string.Join(", ", opened)} now Open");
            if (closed.Count > 0) parts.Add($"{string.Join(", ", closed)} now Closed");
            _trayIcon.ShowBalloonTip(4000, "Tradewatch", string.Join(" | ", parts), WinForms.ToolTipIcon.Info);
        }

        private void OpenExchangeSelector()
        {
            if (_selectorWindow != null && _selectorWindow.IsLoaded)
            {
                _selectorWindow.Activate();
                return;
            }

            _selectorWindow = new ExchangeSelectorWindow(
                _vm.AllExchanges,
                _vm.IsDark,
                () => { _vm.RefreshDisplayed(); _vm.SaveEnabledExchanges(); }
            );
            _selectorWindow.Owner = this;
            _selectorWindow.Closed += (s, e) => _selectorWindow = null;
            _selectorWindow.Show();
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "Tradewatch\n\nA simple app to track global market open hours.\nCreated by Jose.",
                "About Tradewatch",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void ApplyTheme(bool isDark)
        {
            _selectorWindow?.ApplyTheme(isDark);

            var dicts = Application.Current.Resources.MergedDictionaries;
            dicts.Clear();
            dicts.Add(new System.Windows.ResourceDictionary
            {
                Source = new Uri($"Themes/{(isDark ? "Dark" : "Light")}Theme.xaml", UriKind.Relative)
            });

            Brush bg, fg, separator, evenRowBg, oddRowBg;
            if (isDark)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                RootGrid.Background = Brushes.Black;
                MenuBar.Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x4E, 0x4A));
                LocalTimeLabel.Foreground = Brushes.White;
                LocalTimeText.Foreground = Brushes.White;
                ExchangeGrid.Background = Brushes.Black;
                ExchangeGrid.Foreground = Brushes.White;
                evenRowBg = Brushes.Black;
                oddRowBg = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                bg = new SolidColorBrush(Color.FromRgb(11, 61, 58));
                fg = Brushes.White;
                separator = new SolidColorBrush(Color.FromRgb(30, 138, 128));
            }
            else
            {
                this.Background = Brushes.WhiteSmoke;
                RootGrid.Background = Brushes.WhiteSmoke;
                MenuBar.Background = new SolidColorBrush(Color.FromRgb(0x5B, 0x8D, 0xB8));
                LocalTimeLabel.Foreground = Brushes.Black;
                LocalTimeText.Foreground = Brushes.Black;
                ExchangeGrid.Background = Brushes.White;
                ExchangeGrid.Foreground = Brushes.Black;
                evenRowBg = Brushes.White;
                oddRowBg = Brushes.WhiteSmoke;
                bg = new SolidColorBrush(Color.FromRgb(0xA8, 0xC4, 0xDC));
                fg = Brushes.Black;
                separator = new SolidColorBrush(Color.FromRgb(0x8B, 0xAE, 0xC8));
            }

            ExchangeGrid.VerticalGridLinesBrush = separator;
            ExchangeGrid.HorizontalGridLinesBrush = separator;

            var selectionBrush = isDark
                ? new SolidColorBrush(Color.FromRgb(0x1D, 0x8C, 0x84))
                : new SolidColorBrush(Color.FromRgb(0x7E, 0xB3, 0xD4));

            ExchangeGrid.AlternationCount = 2;

            var oddRowTrigger = new DataTrigger
            {
                Binding = new Binding("(ItemsControl.AlternationIndex)") { RelativeSource = RelativeSource.Self },
                Value = 1
            };
            oddRowTrigger.Setters.Add(new Setter(DataGridRow.BackgroundProperty, oddRowBg));

            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, evenRowBg));
            rowStyle.Triggers.Add(oddRowTrigger);
            rowStyle.Triggers.Add(new Trigger
            {
                Property = DataGridRow.IsSelectedProperty,
                Value = true,
                Setters = { new Setter(DataGridRow.BackgroundProperty, selectionBrush) }
            });
            ExchangeGrid.RowStyle = rowStyle;

            ExchangeGrid.Resources[SystemColors.HighlightBrushKey] = selectionBrush;
            ExchangeGrid.Resources[SystemColors.HighlightTextBrushKey] = fg;
            ExchangeGrid.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = selectionBrush;
            ExchangeGrid.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = fg;

            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent));
            cellStyle.Setters.Add(new Setter(DataGridCell.FocusVisualStyleProperty, null));
            cellStyle.Triggers.Add(new Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true,
                Setters = { new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent) }
            });
            ExchangeGrid.CellStyle = cellStyle;

            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, bg));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, fg));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, separator));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
            ExchangeGrid.ColumnHeaderStyle = headerStyle;

            var centeredHeaderStyle = new Style(typeof(DataGridColumnHeader));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, bg));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, fg));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, separator));
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));

            foreach (var col in ExchangeGrid.Columns.Skip(1))
                col.HeaderStyle = centeredHeaderStyle;
        }

        private System.Drawing.Icon CreateCircleIcon(System.Drawing.Color color)
        {
            using var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.Transparent);
                using var brush = new System.Drawing.SolidBrush(color);
                g.FillEllipse(brush, 1, 1, 14, 14);
            }
            IntPtr hIcon = bmp.GetHicon();
            var icon = (System.Drawing.Icon)System.Drawing.Icon.FromHandle(hIcon).Clone();
            DestroyIcon(hIcon);
            return icon;
        }
    }
}
