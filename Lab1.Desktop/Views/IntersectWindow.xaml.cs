using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Lab1.Desktop.Services;
using Lab1.Models;

namespace Lab1.Desktop.Views
{
    public partial class IntersectTablesWindow : Window
    {
        private readonly Guid _databaseId;
        private readonly TableService _tableService = new();

        private List<Table> _tables = new();

        public IntersectTablesWindow(Guid databaseId)
        {
            InitializeComponent();
            _databaseId = databaseId;

            _ = LoadTablesAsync();
        }

        private async Task LoadTablesAsync()
        {
            try
            {
                _tables = await _tableService.GetTablesAsync(_databaseId);
                TableACombo.ItemsSource = _tables;
                TableBCombo.ItemsSource = _tables;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load tables: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }


        private async void TableSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ColumnsList.ItemsSource = null;

            if (TableACombo.SelectedItem is not Table ta ||
                TableBCombo.SelectedItem is not Table tb)
            {
                return; 
            }

            try
            {
                var tableA = await _tableService.GetTableAsync(_databaseId, ta.Id);
                var tableB = await _tableService.GetTableAsync(_databaseId, tb.Id);

                if (tableA == null || tableB == null)
                    return;

                var commonNames = tableA.Columns
                    .Select(c => c.Name)
                    .Intersect(tableB.Columns.Select(c => c.Name))
                    .OrderBy(n => n)
                    .ToList();

                ColumnsList.ItemsSource = commonNames;

                ColumnsList.SelectAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load columns: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (TableACombo.SelectedItem is not Table ta ||
                TableBCombo.SelectedItem is not Table tb)
            {
                MessageBox.Show("Select both tables.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ta.Id == tb.Id)
            {
                MessageBox.Show("Tables A and B must be different.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultName = ResultNameBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(resultName))
            {
                MessageBox.Show("Enter result table name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<string> selectedColumns;

            if (ColumnsList.SelectedItems.Count == 0)
            {
                selectedColumns = ColumnsList.Items.Cast<string>().ToList();
            }
            else
            {
                selectedColumns = ColumnsList.SelectedItems.Cast<string>().ToList();
            }

            try
            {
                await _tableService.IntersectTablesAsync(
                    _databaseId,
                    ta.Id,
                    tb.Id,
                    resultName,
                    selectedColumns);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating intersection: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
