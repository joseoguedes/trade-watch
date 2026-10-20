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
        private ExchangeSelectorWindow? _selectorWindow;
        private WinForms.NotifyIcon _trayIcon = null!;
        private bool _isExiting;
        private System.Drawing.Icon _iconOpen;
        private System.Drawing.Icon _iconClosed;
        private System.Drawing.Icon _iconHoliday;
        private System.Windows.Data.ListCollectionView _pinnedView = null!;
        private double _savedWidth = 700;
        private double _savedHeight = 500;

        public MainWindow()
        {
            InitializeComponent();

            _vm = new MainViewModel();
            DataContext = _vm;

            ApplyGrouping();
            SetupGridStyles();
            SetupCompactView();
            foreach (var ex in _vm.AllExchanges)
                ex.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Exchange.IsPinned))
                        _pinnedView.Refresh();
                };
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.DisplayedExchanges) ||
                    e.PropertyName == nameof(MainViewModel.GroupByRegion))
                    ApplyGrouping();
                if (e.PropertyName == nameof(MainViewModel.DisplayedExchanges))
                    SetupCompactView();
                if (e.PropertyName == nameof(MainViewModel.IsCompactMode))
                    SwitchMode();
                if (e.PropertyName == nameof(MainViewModel.ShowTimeline))
                    ApplyTimelineVisibility(resizeWindow: true);
            };
            ApplyTimelineVisibility(resizeWindow: false);

            if (_vm.IsCompactMode)
                Loaded += (s, e) => SwitchMode();

            var s = _vm.Settings;
            // Restore position only if it is still on a connected screen (left-hand
            // monitors have negative coordinates; an unplugged monitor would hide the window).
            bool hasSavedPos = !(s.WindowLeft == -1 && s.WindowTop == -1);
            bool onScreen = s.WindowLeft + 100 > SystemParameters.VirtualScreenLeft
                         && s.WindowLeft < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 100
                         && s.WindowTop >= SystemParameters.VirtualScreenTop
                         && s.WindowTop < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 50;
            if (hasSavedPos && onScreen) { Left = s.WindowLeft; Top = s.WindowTop; }
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
            _iconHoliday = CreateCircleIcon(System.Drawing.Color.Orange);
            InitializeTrayIcon();

            var initialState = _vm.AllExchanges.Any(ex => ex.IsEnabled && ex.Status == MarketStatus.Open) ? MarketStatus.Open
                             : _vm.AllExchanges.Any(ex => ex.IsEnabled && ex.Status == MarketStatus.Holiday) ? MarketStatus.Holiday
                             : MarketStatus.Closed;
            UpdateTrayIcon(initialState);

            ApplyTheme(_vm.IsDark);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // In compact mode, remember the full-mode size rather than the small compact one.
            if (_vm.IsCompactMode)
                _vm.SaveWindowPosition(Left, Top, _savedWidth, _savedHeight);
            else
                _vm.SaveWindowPosition(Left, Top, Width, Height);

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

        private void UnsubscribeEvents()
        {
            _vm.ThemeChanged -= ApplyTheme;
            _vm.AnyOpenChanged -= UpdateTrayIcon;
            _vm.StatusChanged -= ShowStatusBalloon;
            _vm.ExitRequested -= ExitApp;
            _vm.ManageExchangesRequested -= OpenExchangeSelector;
            _vm.AboutRequested -= ShowAbout;
        }

        private void ExitApp()
        {
            _isExiting = true;
            UnsubscribeEvents();
            _vm.StopTimer();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            _iconOpen?.Dispose();
            _iconClosed?.Dispose();
            _iconHoliday?.Dispose();
            base.OnClosed(e);
        }

        private void UpdateTrayIcon(MarketStatus marketState)
        {
            _trayIcon.Icon = marketState == MarketStatus.Open ? _iconOpen
                           : marketState == MarketStatus.Holiday ? _iconHoliday
                           : _iconClosed;
            _trayIcon.Text = marketState == MarketStatus.Open ? "Tradewatch — markets open"
                           : marketState == MarketStatus.Holiday ? "Tradewatch — markets on holiday"
                           : "Tradewatch — all markets closed";
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
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            MessageBox.Show(
                $"Tradewatch {version?.ToString(3)}\n\nA simple app to track global market open hours.\nCreated by Jose.",
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

            if (isDark)
            {
                this.Background = new SolidColorBrush(Color.FromRgb(17, 17, 17));
                RootGrid.Background = Brushes.Black;
                MenuBar.Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x4E, 0x4A));
                LocalTimeLabel.Foreground = Brushes.White;
                LocalTimeText.Foreground = Brushes.White;
                ExchangeGrid.Background = Brushes.Black;
                ExchangeGrid.Foreground = Brushes.White;
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
            }

            // Compact panel theming
            var compactHeaderBg = isDark
                ? new SolidColorBrush(Color.FromRgb(0x0E, 0x4E, 0x4A))
                : new SolidColorBrush(Color.FromRgb(0x5B, 0x8D, 0xB8));
            CompactHeader.Background = compactHeaderBg;
            CompactList.Foreground = isDark ? Brushes.White : Brushes.Black;
            CompactList.Background = isDark ? Brushes.Black : Brushes.White;
            CompactFooter.Background = isDark
                ? new SolidColorBrush(Color.FromRgb(0x0A, 0x1E, 0x1C))
                : new SolidColorBrush(Color.FromRgb(0xE8, 0xF4, 0xF0));
            CompactFooterText.Foreground = isDark
                ? new SolidColorBrush(Color.FromRgb(0x77, 0x88, 0x88))
                : new SolidColorBrush(Color.FromRgb(0x60, 0x70, 0x70));

            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                var vScrollBar = FindVisualDescendants<System.Windows.Controls.Primitives.ScrollBar>(ExchangeGrid)
                    .FirstOrDefault(s => s.Orientation == Orientation.Vertical);
                if (vScrollBar == null) return;

                // The DataGrid template places the vertical scrollbar at row 0, rowspan 2,
                // which puts its top (the up-arrow button) inside the column header row.
                // Moving it to row 1 only makes it start at the data area — no more square.
                Grid.SetRow(vScrollBar, 1);
                Grid.SetRowSpan(vScrollBar, 1);
            }));
        }

        // Built once: the styles pull their colours from the theme dictionary via
        // DynamicResource, so switching theme only swaps brushes instead of
        // re-templating every row.
        private void SetupGridStyles()
        {
            ExchangeGrid.SetResourceReference(DataGrid.VerticalGridLinesBrushProperty, "GridLineBrush");
            ExchangeGrid.SetResourceReference(DataGrid.HorizontalGridLinesBrushProperty, "GridLineBrush");
            ExchangeGrid.AlternationCount = 2;

            var oddRowTrigger = new DataTrigger
            {
                Binding = new Binding("(ItemsControl.AlternationIndex)") { RelativeSource = RelativeSource.Self },
                Value = 1
            };
            oddRowTrigger.Setters.Add(new Setter(DataGridRow.BackgroundProperty, new DynamicResourceExtension("GridRowOddBrush")));

            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(DataGridRow.BackgroundProperty, new DynamicResourceExtension("GridRowEvenBrush")));
            rowStyle.Triggers.Add(oddRowTrigger);
            rowStyle.Triggers.Add(new Trigger
            {
                Property = DataGridRow.IsSelectedProperty,
                Value = true,
                Setters = { new Setter(DataGridRow.BackgroundProperty, new DynamicResourceExtension("GridSelectionBrush")) }
            });
            ExchangeGrid.RowStyle = rowStyle;

            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.Transparent));
            cellStyle.Setters.Add(new Setter(DataGridCell.FocusVisualStyleProperty, null));
            cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new DynamicResourceExtension("GridHeaderFgBrush")));
            cellStyle.Triggers.Add(new Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true,
                Setters = { new Setter(DataGridCell.BackgroundProperty, Brushes.Transparent) }
            });
            ExchangeGrid.CellStyle = cellStyle;

            var headerStyle = new Style(typeof(DataGridColumnHeader));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BackgroundProperty, new DynamicResourceExtension("GridHeaderBgBrush")));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.ForegroundProperty, new DynamicResourceExtension("GridHeaderFgBrush")));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderBrushProperty, new DynamicResourceExtension("GridLineBrush")));
            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.BorderThicknessProperty, new Thickness(0, 0, 1, 0)));
            ExchangeGrid.ColumnHeaderStyle = headerStyle;

            var centeredHeaderStyle = new Style(typeof(DataGridColumnHeader), headerStyle);
            centeredHeaderStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
            foreach (var col in ExchangeGrid.Columns.Where((_, i) => i != 1))
                col.HeaderStyle = centeredHeaderStyle;
        }

        private void SetupCompactView()
        {
            _pinnedView = new System.Windows.Data.ListCollectionView(_vm.DisplayedExchanges);
            _pinnedView.Filter = o => o is Exchange ex && ex.IsPinned;
            CompactList.ItemsSource = _pinnedView;
        }

        private void SwitchMode()
        {
            if (_vm.IsCompactMode)
            {
                _savedWidth = Width;
                _savedHeight = Height;

                FullModePanel.Visibility = Visibility.Collapsed;
                CompactPanel.Visibility = Visibility.Visible;

                int pinnedCount = _vm.AllExchanges.Count(e => e.IsPinned && e.IsEnabled);
                Width = 300;
                Height = Math.Max(160, Math.Min(480, 50 + pinnedCount * 40 + 36));
                ResizeMode = ResizeMode.CanResize;
                // SetCurrentValue keeps the Topmost → AlwaysOnTop binding alive;
                // a plain assignment would silently break the menu toggle.
                SetCurrentValue(TopmostProperty, true);
            }
            else
            {
                FullModePanel.Visibility = Visibility.Visible;
                CompactPanel.Visibility = Visibility.Collapsed;

                Width = _savedWidth;
                Height = _savedHeight;
                SetCurrentValue(TopmostProperty, _vm.AlwaysOnTop);
                ResizeMode = ResizeMode.CanResize;
            }
        }

        private void NextToOpenName_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && _vm.NextToOpenExchange is Exchange ex)
                RevealExchange(ex);
        }

        // Scroll the main list to an exchange and select it, expanding its region
        // group first if the user had collapsed it.
        private void RevealExchange(Exchange ex)
        {
            if (!_vm.DisplayedExchanges.Contains(ex)) return;

            ExchangeGrid.ScrollIntoView(ex);
            ExchangeGrid.UpdateLayout();
            var group = FindVisualDescendants<GroupItem>(ExchangeGrid)
                .FirstOrDefault(g => g.DataContext is CollectionViewGroup cvg && cvg.Items.Contains(ex));
            if (group != null
                && FindVisualDescendants<ToggleButton>(group).FirstOrDefault(t => t.Name == "Toggle") is ToggleButton toggle
                && toggle.IsChecked == false)
            {
                toggle.IsChecked = true;
                ExchangeGrid.UpdateLayout();
            }

            ExchangeGrid.SelectedItem = ex;
            ExchangeGrid.ScrollIntoView(ex);
            ExchangeGrid.Focus();
        }

        private void ApplyTimelineVisibility(bool resizeWindow)
        {
            TimelineColumn.Visibility = _vm.ShowTimeline ? Visibility.Visible : Visibility.Collapsed;
            if (!resizeWindow || _vm.IsCompactMode) return;
            // Make room for the extra column rather than squeezing the exchange names.
            if (_vm.ShowTimeline) Width += TimelineColumn.Width.Value;
            else Width = Math.Max(MinWidth, Width - TimelineColumn.Width.Value);
        }

        private void ApplyGrouping()
        {
            var view = CollectionViewSource.GetDefaultView(_vm.DisplayedExchanges);
            view.GroupDescriptions.Clear();
            if (_vm.GroupByRegion)
                view.GroupDescriptions.Add(new PropertyGroupDescription("Region"));
        }

        private static IEnumerable<T> FindVisualDescendants<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var desc in FindVisualDescendants<T>(child))
                    yield return desc;
            }
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
