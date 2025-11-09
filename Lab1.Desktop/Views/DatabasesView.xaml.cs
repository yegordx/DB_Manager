using System.Windows.Controls;
using Lab1.Desktop.ViewModels;

namespace Lab1.Desktop.Views
{
    public partial class DatabasesView : UserControl
    {
        public DatabasesView(MainWindow mainWindow)
        {
            InitializeComponent();
            DataContext = new DatabasesViewModel(mainWindow);
        }
    }
}
