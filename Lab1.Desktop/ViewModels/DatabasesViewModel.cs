using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab1.Desktop.Services;
using Lab1.Desktop.Views;
using Lab1.Models;

namespace Lab1.Desktop.ViewModels
{
    public partial class DatabasesViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService = new();
        private readonly MainWindow _mainWindow;

        [ObservableProperty]
        private ObservableCollection<Database> databases = new();

        [ObservableProperty]
        private Database? selectedDatabase;

        [ObservableProperty]
        private string newDatabaseName = string.Empty;

        public DatabasesViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            CreateCommand = new AsyncRelayCommand(CreateAsync);
            DeleteCommand = new AsyncRelayCommand<Database>(DeleteAsync);
            OpenCommand = new RelayCommand<Database>(OpenTables);

            _ = LoadAsync();
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand CreateCommand { get; }
        public IAsyncRelayCommand<Database> DeleteCommand { get; }
        public IRelayCommand<Database> OpenCommand { get; }

        private async Task LoadAsync()
        {
            try
            {
                var list = await _dbService.GetAllAsync();
                Databases = new ObservableCollection<Database>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load databases: {ex.Message}");
            }
        }

        private async Task CreateAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDatabaseName))
            {
                MessageBox.Show("Please enter a database name.");
                return;
            }

            try
            {
                await _dbService.CreateAsync(NewDatabaseName);
                NewDatabaseName = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating database: {ex.Message}");
            }
        }

        private async Task DeleteAsync(Database? db)
        {
            if (db == null) return;

            if (MessageBox.Show($"Delete database '{db.Name}'?",
                                "Confirm",
                                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                await _dbService.DeleteDatabaseAsync(db.Id);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting database: {ex.Message}");
            }
        }

        private void OpenTables(Database? db)
        {
            if (db == null) return;
            _mainWindow.NavigateTo(new TablesView(_mainWindow, db));
        }
    }
}
