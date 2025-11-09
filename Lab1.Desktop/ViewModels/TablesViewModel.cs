using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab1.Desktop.Services;
using Lab1.Models;
using Lab1.Desktop.Views;

namespace Lab1.Desktop.ViewModels
{
    public partial class TablesViewModel : ObservableObject
    {
        private readonly TableService _tableService = new();
        private readonly MainWindow _mainWindow;
        private readonly Database _database;

        [ObservableProperty] private ObservableCollection<Table> tables = new();
        [ObservableProperty] private Table? selectedTable;
        [ObservableProperty] private string newTableName = string.Empty;

        public TablesViewModel(MainWindow mainWindow, Database database)
        {
            _mainWindow = mainWindow;
            _database = database;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            CreateCommand = new AsyncRelayCommand(CreateAsync);
            DeleteCommand = new AsyncRelayCommand<Table>(DeleteAsync);
            OpenRowsCommand = new RelayCommand<Table>(OpenRows);
            OpenColumnsCommand = new RelayCommand<Table>(OpenColumns);
            BackCommand = new RelayCommand(GoBack);

            OpenIntersectDialogCommand = new RelayCommand(OpenIntersectDialog);

            _ = LoadAsync();
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand CreateCommand { get; }
        public IAsyncRelayCommand<Table> DeleteCommand { get; }
        public IRelayCommand<Table> OpenRowsCommand { get; }
        public IRelayCommand<Table> OpenColumnsCommand { get; }
        public IRelayCommand BackCommand { get; }

        public IRelayCommand OpenIntersectDialogCommand { get; }   // 🔹

        private async Task LoadAsync()
        {
            try
            {
                var result = await _tableService.GetTablesAsync(_database.Id);
                Tables = new ObservableCollection<Table>(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tables: {ex.Message}");
            }
        }

        private async Task CreateAsync()
        {
            if (string.IsNullOrWhiteSpace(NewTableName))
            {
                MessageBox.Show("Please enter table name.");
                return;
            }

            try
            {
                await _tableService.CreateTableAsync(_database.Id, NewTableName);
                NewTableName = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating table: {ex.Message}");
            }
        }

        private async Task DeleteAsync(Table? table)
        {
            if (table == null) return;

            if (MessageBox.Show(
                    $"Delete table '{table.Name}'?",
                    "Confirm",
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                await _tableService.DeleteTableAsync(_database.Id, table.Id);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting table: {ex.Message}");
            }
        }

        private void OpenRows(Table? table)
        {
            if (table == null) return;
            _mainWindow.NavigateTo(new RowsView(_mainWindow, _database, table));
        }

        private void OpenColumns(Table? table)
        {
            if (table == null) return;

            var win = new ColumnsWindow(_database.Id, table.Id)
            {
                Owner = Application.Current.MainWindow
            };
            win.ShowDialog();

            _ = LoadAsync();
        }

        private void GoBack()
        {
            _mainWindow.NavigateBack();
        }

        private void OpenIntersectDialog()
        {
            var win = new IntersectTablesWindow(_database.Id)
            {
                Owner = Application.Current.MainWindow
            };

            var result = win.ShowDialog();
            if (result == true)
            {
                _ = LoadAsync();
            }
        }
    }
}
