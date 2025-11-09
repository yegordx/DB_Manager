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
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand CreateCommand { get; }
        public IAsyncRelayCommand<Table> DeleteCommand { get; }
        public IRelayCommand<Table> OpenRowsCommand { get; }
        public IRelayCommand BackCommand { get; }

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
                await LoadAsync();
                NewTableName = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating table: {ex.Message}");
            }
        }

        private async Task DeleteAsync(Table? table)
        {
            if (table == null) return;

            if (MessageBox.Show($"Delete table '{table.Name}'?",
                "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

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

        private void GoBack()
        {
            _mainWindow.NavigateBack();
        }
    }
}
