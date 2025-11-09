using System.Windows.Controls;
using Lab1.Models;
using Lab1.Desktop.ViewModels;

namespace Lab1.Desktop.Views
{
    public partial class TablesView : UserControl
    {
        public TablesView(MainWindow mainWindow, Database db)
        {
            InitializeComponent();
            DataContext = new TablesViewModel(mainWindow, db);
        }
    }
}
