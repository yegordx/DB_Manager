using CommunityToolkit.Mvvm.ComponentModel;
using Lab1.Desktop.Views;

namespace Lab1.Desktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly MainWindow _window;

        [ObservableProperty]
        private object? currentView;

        public MainViewModel(MainWindow window)
        {
            _window = window;

            CurrentView = new DatabasesView(_window);
        }

        public void NavigateTo(object view)
        {
            CurrentView = view;
        }

        public void NavigateBack()
        {
            CurrentView = new DatabasesView(_window);
        }
    }
}
