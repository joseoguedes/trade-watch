using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Tradewatch;

namespace Tradewatch.ViewModels
{
    public class ExchangeSelectorViewModel : INotifyPropertyChanged
    {
        private readonly ObservableCollection<Exchange> _allExchanges;
        private readonly Action _onSave;
        private readonly ObservableCollection<ExchangeItem> _items = new();
        private string _searchText = "";

        public ICollectionView ItemsView { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ItemsView.Refresh();
            }
        }

        public event Action CloseRequested;

        public ICommand SaveCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand CancelCommand { get; }

        public ExchangeSelectorViewModel(ObservableCollection<Exchange> allExchanges, Action onSave)
        {
            _allExchanges = allExchanges;
            _onSave = onSave;

            foreach (var ex in allExchanges)
                _items.Add(new ExchangeItem { Name = ex.Name, IsChecked = ex.IsEnabled });

            ItemsView = CollectionViewSource.GetDefaultView(_items);
            ItemsView.Filter = obj => obj is ExchangeItem item &&
                (string.IsNullOrWhiteSpace(_searchText) ||
                 item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase));

            SaveCommand = new RelayCommand(Save);
            SelectAllCommand = new RelayCommand(() => { foreach (var i in _items) i.IsChecked = true; });
            ClearAllCommand = new RelayCommand(() => { foreach (var i in _items) i.IsChecked = false; });
            CancelCommand = new RelayCommand(() => CloseRequested?.Invoke());
        }

        private void Save()
        {
            foreach (var item in _items)
            {
                var ex = _allExchanges.FirstOrDefault(e => e.Name == item.Name);
                if (ex != null) ex.IsEnabled = item.IsChecked;
            }
            _onSave();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ExchangeItem : INotifyPropertyChanged
    {
        private bool _isChecked;
        public string Name { get; set; }

        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
