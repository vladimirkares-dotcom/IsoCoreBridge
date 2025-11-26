using IsoCore.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public sealed partial class ProjectDetailPage : MenuPage
{
    public ProjectDetailPageViewModel ViewModel { get; }

    public ProjectDetailPage()
    {
        InitializeComponent();
        ViewModel = new ProjectDetailPageViewModel(App.AppState);
        DataContext = ViewModel;
    }

    private void OnBackButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo<ProjectsPage>();
    }
}
