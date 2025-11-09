using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab1.Desktop.Services;
using Lab1.Models;
using Lab1.Desktop.Views;

namespace Lab1.Desktop.ViewModels
{
    public partial class RowsViewModel : ObservableObject
    {
        private readonly RowService _rowService = new();
        private readonly MainWindow _mainWindow;
        private readonly Database _database;
        private readonly Table _table;

        [ObservableProperty] private ObservableCollection<RowItem> rows = new();
        [ObservableProperty] private RowItem? selectedRow;

        public RowsViewModel(MainWindow mainWindow, Database db, Table table)
        {
            _mainWindow = mainWindow;
            _database = db;
            _table = table;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            AddCommand = new AsyncRelayCommand(AddAsync);
            UpdateCommand = new AsyncRelayCommand(UpdateAsync);
            DeleteCommand = new AsyncRelayCommand(DeleteAsync);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public IAsyncRelayCommand LoadCommand { get; }
        public IAsyncRelayCommand AddCommand { get; }
        public IAsyncRelayCommand UpdateCommand { get; }
        public IAsyncRelayCommand DeleteCommand { get; }
        public IRelayCommand BackCommand { get; }

        private async Task LoadAsync()
        {
            try
            {
                var list = await _rowService.GetRowsAsync(_database.Id, _table.Id);

                Rows = new ObservableCollection<RowItem>(
                    list.Select(r => new RowItem
                    {
                        Id = r.Id,
                        ValuesJson = JsonSerializer.Serialize(r.Values, new JsonSerializerOptions { WriteIndented = true })
                    }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load rows: {ex.Message}");
            }
        }

        private async Task AddAsync()
        {
            var json = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter JSON for new row (e.g. {\"Name\": \"John\", \"Age\": 25})",
                "Add Row", "{}");

            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (dict == null) return;

                await _rowService.AddRowAsync(_database.Id, _table.Id, dict);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding row: {ex.Message}");
            }
        }

        private async Task UpdateAsync()
        {
            if (SelectedRow == null)
            {
                MessageBox.Show("Select a row to update.");
                return;
            }

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(SelectedRow.ValuesJson);
                if (dict == null) return;

                await _rowService.UpdateRowAsync(_database.Id, _table.Id, SelectedRow.Id, dict);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating row: {ex.Message}");
            }
        }

        private async Task DeleteAsync()
        {
            if (SelectedRow == null) return;

            if (MessageBox.Show("Delete this row?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                await _rowService.DeleteRowAsync(_database.Id, _table.Id, SelectedRow.Id);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting row: {ex.Message}");
            }
        }

        private void GoBack()
        {
            _mainWindow.NavigateTo(new TablesView(_mainWindow, _database));
        }

        public class RowItem
        {
            public Guid Id { get; set; }
            public string ValuesJson { get; set; } = "{}";
        }
    }
}
