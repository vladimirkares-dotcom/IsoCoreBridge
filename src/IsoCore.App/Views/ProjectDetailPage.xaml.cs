using System.Diagnostics;
using IsoCore.App.ViewModels;
using IsoCore.Domain;
using Microsoft.UI.Xaml;
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
        Loaded += OnPageLoaded;
    }

    private void OnBackButtonClicked(object sender, RoutedEventArgs e)
    {
        NavigateTo<ProjectsPage>();
    }

    private void OnEditBuildingObjectButtonClick(object sender, RoutedEventArgs e)
    {
        var vm = DataContext as ProjectDetailPageViewModel;
        var selected = vm?.SelectedBuildingObject;
        var project = App.AppState.CurrentProject;

        if (selected == null || project == null)
        {
            return;
        }

        App.AppState.CurrentBuildingObjectEditSession = new BuildingObjectEditSession
        {
            Mode = BuildingObjectEditMode.Edit,
            Project = project,
            Target = selected
        };

        NavigateTo<BuildingObjectDetailPage>();
    }

    private void OnNewBuildingObjectButtonClick(object sender, RoutedEventArgs e)
    {
        var project = ViewModel.CurrentProject ?? App.AppState.CurrentProject;
        if (project == null)
        {
            Debug.WriteLine("OnNewBuildingObjectButtonClick: No current project found.");
            return;
        }

        var newObject = new BuildingObjectInfo
        {
            Code = string.Empty,
            Name = string.Empty,
            StructureType = BuildingStructureType.Unknown,
            CoverType = BuildingCoverType.Unknown,
            Status = BuildingObjectStatus.Draft,
            PrepType = SurfacePrepType.Unknown,
            HasNaip = true,
            Notes = string.Empty
        };

        App.AppState.CurrentBuildingObjectEditSession = new BuildingObjectEditSession
        {
            Project = project,
            Target = newObject,
            Mode = BuildingObjectEditMode.New
        };

        NavigateTo<BuildingObjectDetailPage>();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnPageLoaded;
        await ViewModel.RefreshCurrentProjectAsync();
    }
}
