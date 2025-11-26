using IsoCore.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public sealed partial class ProjectDetailPage : Page
{
    public ProjectDetailPageViewModel ViewModel { get; }

    public ProjectDetailPage()
    {
        InitializeComponent();
        ViewModel = new ProjectDetailPageViewModel(App.AppState);
        DataContext = ViewModel;
    }
}
