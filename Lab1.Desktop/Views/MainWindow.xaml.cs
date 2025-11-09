using System.Windows;
using Lab1.Desktop.ViewModels;

namespace Lab1.Desktop.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(this);
        }

        public void NavigateTo(object view)
        {
            if (DataContext is MainViewModel vm)
                vm.CurrentView = view;
        }

        public void NavigateBack()
        {
            if (DataContext is MainViewModel vm)
                vm.NavigateBack();
        }
    }
}