using IsoCore.App.ViewModels;
using IsoCore.Domain;
using Microsoft.UI.Xaml;

namespace IsoCore.App.Views;

public sealed partial class BuildingObjectDetailPage : MenuPage
{
    public BuildingObjectDetailPage()
    {
        InitializeComponent();
        ViewModel = new BuildingObjectDetailPageViewModel();
        DataContext = ViewModel;

        var session = App.AppState.CurrentBuildingObjectEditSession;
        if (session != null &&
            session.Mode == BuildingObjectEditMode.Edit &&
            session.Target != null)
        {
            ViewModel.LoadFrom(session.Target);
        }
    }

    public BuildingObjectDetailPageViewModel ViewModel { get; }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        App.AppState.CurrentBuildingObjectEditSession = null;
        NavigateTo<ProjectDetailPage>();
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var session = App.AppState.CurrentBuildingObjectEditSession;
        if (session?.Target == null)
        {
            App.AppState.CurrentBuildingObjectEditSession = null;
            NavigateTo<ProjectDetailPage>();
            return;
        }

        // Copy values from the edit ViewModel into the target BuildingObjectInfo
        ViewModel.ApplyTo(session.Target);

        // When creating a new object, add it to the project collection
        if (session.Mode == BuildingObjectEditMode.New && session.Project != null)
        {
            var collection = session.Project.BuildingObjects;
            if (collection != null && !collection.Contains(session.Target))
            {
                collection.Add(session.Target);
            }
        }

        if (session.Mode == BuildingObjectEditMode.New && session.Project != null)
        {
            var collection = session.Project.BuildingObjects;
            if (collection != null && !collection.Contains(session.Target))
            {
                collection.Add(session.Target);
            }
        }

        if (session.Project != null)
        {
            await App.AppState.ProjectRegistry.UpdateProjectAsync(session.Project);
        }

        App.AppState.CurrentBuildingObjectEditSession = null;
        NavigateTo<ProjectDetailPage>();
    }
}
