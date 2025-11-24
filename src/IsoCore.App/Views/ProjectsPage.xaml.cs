using IsoCore.App.Services;
using IsoCore.App.ViewModels;
using IsoCore.Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public sealed partial class ProjectsPage : Page
{
    private readonly IAppStateService _appState;
    public ProjectsViewModel ViewModel { get; }

    public ProjectsPage()
    {
        InitializeComponent();
        _appState = App.AppState;
        ViewModel = new ProjectsViewModel(_appState);
        DataContext = ViewModel;
        Loaded += ProjectsPage_Loaded;
    }

    private async void ProjectsPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ProjectsPage_Loaded;
        await ViewModel.LoadProjectsAsync().ConfigureAwait(true);
    }

    private void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ProjectsViewModel vm)
        {
            return;
        }

        var selected = (sender as ListView)?.SelectedItem as ProjectInfo;
        if (selected != null)
        {
            vm.SetCurrentProject(selected);
        }
    }

    private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProjectsViewModel vm)
        {
            return;
        }

        var selected = vm.SelectedProject;
        if (selected == null)
        {
            return;
        }

        vm.OpenProject(selected);

        // TODO: navigate to ProjectDetailPage when navigation shell is available.
    }
}
