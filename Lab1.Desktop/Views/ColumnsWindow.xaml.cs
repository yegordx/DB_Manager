using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Lab1.Desktop.Services;
using Lab1.Models;

namespace Lab1.Desktop.Views
{
    public partial class ColumnsWindow : Window
    {
        private readonly Guid _databaseId;
        private readonly Guid _tableId;
        private readonly TableService _tableService = new();

        public ColumnsWindow(Guid databaseId, Guid tableId)
        {
            InitializeComponent();
            _databaseId = databaseId;
            _tableId = tableId;

            TypeBox.ItemsSource = Enum.GetNames(typeof(FieldType));
            TypeBox.SelectedIndex = 0;

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var table = await _tableService.GetTableAsync(_databaseId, _tableId);
                if (table == null)
                {
                    MessageBox.Show("Table not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                ColumnsList.ItemsSource = table.Columns.OrderBy(c => c.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load columns: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAsync();
        }

        // 🔹 Показуємо / ховаємо панель інтервалу
        private void TypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var typeStr = TypeBox.SelectedItem as string;

            if (typeStr == nameof(FieldType.CharInvl) ||
                typeStr == nameof(FieldType.StringCharInvl))
            {
                IntervalPanel.Visibility = Visibility.Visible;
            }
            else
            {
                IntervalPanel.Visibility = Visibility.Collapsed;
                StartBox.Text = string.Empty;
                EndBox.Text = string.Empty;
            }
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var name = NameBox.Text?.Trim();
            var typeStr = TypeBox.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter column name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(typeStr))
            {
                MessageBox.Show("Select column type.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            char? start = null;
            char? end = null;

            // 🔹 Якщо обрано інтервальний тип — обов'язково вимагаємо інтервал
            if (typeStr == nameof(FieldType.CharInvl) ||
                typeStr == nameof(FieldType.StringCharInvl))
            {
                if (string.IsNullOrWhiteSpace(StartBox.Text) ||
                    string.IsNullOrWhiteSpace(EndBox.Text))
                {
                    MessageBox.Show("For CharInvl / StringCharInvl you must specify start and end chars.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                start = StartBox.Text[0];
                end = EndBox.Text[0];

                if (end < start)
                {
                    MessageBox.Show("End char must be greater or equal to start char.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                if (start.HasValue && end.HasValue)
                {
                    await _tableService.AddColumnAsync(_databaseId, _tableId, name, typeStr!, start.Value, end.Value);
                }
                else
                {
                    await _tableService.AddColumnAsync(_databaseId, _tableId, name, typeStr!);
                }

                NameBox.Text = string.Empty;
                StartBox.Text = string.Empty;
                EndBox.Text = string.Empty;

                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add column: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ColumnsList.SelectedItem is not Column col)
            {
                MessageBox.Show("Select a column to delete.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete column '{col.Name}'?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await _tableService.DeleteColumnAsync(_databaseId, _tableId, col.Id);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete column: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}