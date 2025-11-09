using System.Windows.Controls;
using Lab1.Desktop.ViewModels;
using Lab1.Models;

namespace Lab1.Desktop.Views
{
    public partial class RowsView : UserControl
    {
        public RowsView(MainWindow mainWindow, Database db, Table table)
        {
            InitializeComponent();
            DataContext = new RowsViewModel(mainWindow, db, table);
        }
    }
}
