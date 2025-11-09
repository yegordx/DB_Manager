using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Lab1.Desktop.Services;
using Lab1.Desktop.ViewModels;
using Lab1.Models;

namespace Lab1.Desktop.Views
{
    public partial class RowsView : UserControl
    {
        private readonly MainWindow _mainWindow;
        private readonly Database _database;
        private Table _table;

        public RowsView(MainWindow mainWindow, Database database, Table table)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
            _database = database;
            _table = table; 

            DataContext = new RowsViewModel(mainWindow, database, table);

            Loaded += RowsView_Loaded;
        }

        private async void RowsView_Loaded(object sender, RoutedEventArgs e)
        {
            var tableService = new TableService();
            try
            {
                var fullTable = await tableService.GetTableAsync(_database.Id, _table.Id);
                if (fullTable != null)
                {
                    _table = fullTable;

                    if (DataContext is RowsViewModel vm)
                    {
                        vm.UpdateTableSchema(fullTable);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load table schema: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            BuildColumns();
        }

        private void BuildColumns()
        {
            RowsGrid.Columns.Clear();

            RowsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Id",
                Binding = new Binding("Id"),
                IsReadOnly = true,
                Width = new DataGridLength(150)
            });

            foreach (var column in _table.Columns)
            {
                RowsGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = column.Name,
                    Binding = new Binding($"Values[{column.Name}]")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                });
            }
        }

        private void RowsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {

        }
    }
}
