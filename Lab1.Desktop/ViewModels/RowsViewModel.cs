using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab1.Desktop.Services;
using Lab1.Desktop.Views;
using Lab1.Models;

namespace Lab1.Desktop.ViewModels
{
    public partial class RowsViewModel : ObservableObject
    {
        private readonly RowService _rowService = new();
        private readonly MainWindow _mainWindow;
        private readonly Database _database;
        private Table _table;

        [ObservableProperty] private ObservableCollection<RowItem> rows = new();
        [ObservableProperty] private RowItem? selectedRow;

        public RowsViewModel(MainWindow mainWindow, Database database, Table table)
        {
            _mainWindow = mainWindow;
            _database = database;
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

        public void UpdateTableSchema(Table table)
        {
            _table = table;
        }

        private async Task LoadAsync()
        {
            try
            {
                var rowsFromApi = await _rowService.GetRowsAsync(_database.Id, _table.Id);
                var items = new ObservableCollection<RowItem>();

                foreach (var row in rowsFromApi)
                {
                    items.Add(new RowItem
                    {
                        Id = row.Id,
                        Values = new Dictionary<string, object>(row.Values)
                    });
                }

                Rows = items;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load rows: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddAsync()
        {
            try
            {
                var values = new Dictionary<string, object>();
                foreach (var column in _table.Columns)
                {
                    values[column.Name] = GetDefaultValue(column.Type);
                }

                await _rowService.AddRowAsync(_database.Id, _table.Id, values);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding row: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateAsync()
        {
            if (SelectedRow == null)
            {
                MessageBox.Show("Select a row to update.",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!TryBuildTypedValues(SelectedRow, out var typedValues))
                    return;

                await _rowService.UpdateRowAsync(
                    _database.Id, _table.Id, SelectedRow.Id, typedValues);

                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating row: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteAsync()
        {
            if (SelectedRow == null) return;

            if (MessageBox.Show("Delete this row?", "Confirm",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                await _rowService.DeleteRowAsync(_database.Id, _table.Id, SelectedRow.Id);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting row: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoBack()
        {
            _mainWindow.NavigateTo(new TablesView(_mainWindow, _database));
        }


        private object GetDefaultValue(FieldType type)
        {
            return type switch
            {
                FieldType.Integer => 0,
                FieldType.Real => 0.0,
                FieldType.Char => '\0',
                FieldType.String => string.Empty,
                FieldType.CharInvl => 'A',        
                FieldType.StringCharInvl => string.Empty,
                _ => string.Empty
            };
        }

        private bool TryBuildTypedValues(RowItem row,
            out Dictionary<string, object> typedValues)
        {
            typedValues = new Dictionary<string, object>();

            foreach (var column in _table.Columns)
            {
                if (!row.Values.TryGetValue(column.Name, out var raw))
                    continue;

                var success = TryConvertForFieldType(column.Type, raw,
                    out var converted, out var error);

                if (!success)
                {
                    MessageBox.Show(
                        $"Column '{column.Name}': {error}",
                        "Validation error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return false;
                }

                typedValues[column.Name] = converted!;
            }

            return true;
        }

        private bool TryConvertForFieldType(
            FieldType type,
            object? raw,
            out object? converted,
            out string? error)
        {
            error = null;
            converted = null;

            if (raw == null)
            {
                error = "Value cannot be empty.";
                return false;
            }

            var s = raw.ToString() ?? string.Empty;
            var ci = CultureInfo.InvariantCulture;

            switch (type)
            {
                case FieldType.Integer:
                    if (int.TryParse(s, NumberStyles.Integer, ci, out var i))
                    {
                        converted = i;
                        return true;
                    }
                    error = "Expected integer value.";
                    return false;

                case FieldType.Real:
                    if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, ci, out var d))
                    {
                        converted = d;
                        return true;
                    }
                    error = "Expected real (number) value.";
                    return false;

                case FieldType.Char:
                case FieldType.CharInvl:
                    if (s.Length == 1)
                    {
                        converted = s[0];
                        return true;
                    }
                    error = "Expected single character.";
                    return false;

                case FieldType.String:
                case FieldType.StringCharInvl:
                    converted = s;
                    return true;

                default:
                    converted = s;
                    return true;
            }
        }

        public class RowItem
        {
            public Guid Id { get; set; }
            public Dictionary<string, object> Values { get; set; } = new();
        }
    }
}
