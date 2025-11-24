using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using IsoCore.App.Services;
using IsoCore.Domain;
using Microsoft.UI.Xaml;

namespace IsoCore.App.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IAppStateService _appState;
    private string _currentProjectName = "(zatím nevybrán)";
    private int _totalProjects;
    private int _preparationCount;
    private int _executionCount;
    private int _completedCount;

    public DashboardViewModel(IAppStateService appState)
    {
        _appState = appState;
        UpdateFromProject(_appState.CurrentProject);
        _appState.CurrentProjectChanged += OnCurrentProjectChanged;
        _appState.CurrentDateChanged += OnDateChanged;
        _appState.CurrentUserChanged += OnCurrentUserChanged;

        if (_appState.ProjectRegistry?.Projects is ObservableCollection<ProjectInfo> projects)
        {
            Projects = projects;
            Projects.CollectionChanged += OnProjectsCollectionChanged;
            UpdateProjectStats(projects);
        }
        else
        {
            Projects = new ObservableCollection<ProjectInfo>();
        }
    }

    public string CurrentProjectName
    {
        get => _currentProjectName;
        private set
        {
            if (_currentProjectName != value)
            {
                _currentProjectName = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<ProjectInfo> Projects { get; }

    public int TotalProjects
    {
        get => _totalProjects;
        private set => SetProperty(ref _totalProjects, value);
    }

    public int PreparationCount
    {
        get => _preparationCount;
        private set => SetProperty(ref _preparationCount, value);
    }

    public int ExecutionCount
    {
        get => _executionCount;
        private set => SetProperty(ref _executionCount, value);
    }

    public int CompletedCount
    {
        get => _completedCount;
        private set => SetProperty(ref _completedCount, value);
    }

    public DateOnly CurrentDate => _appState.CurrentDate;

    public string CurrentProjectCode =>
        _appState.CurrentProject?.ProjectCode ?? "-";

    public string CurrentProjectOnlyName =>
        _appState.CurrentProject?.ProjectName ?? "-";

    public string CurrentProjectStatus =>
        _appState.CurrentProject != null
            ? "Probíhající realizace"
            : "Žádný projekt není aktivní";

    public string CurrentProjectStatusName =>
        _appState.CurrentProject?.Status switch
        {
            ProjectStatus.Preparation => "V přípravě",
            ProjectStatus.Execution => "V realizaci",
            ProjectStatus.Completed => "Dokončeno",
            _ => "Neznámý stav"
        };

    public bool CanViewAdvancedBlocks =>
        string.Equals(_appState.CurrentUser?.Role, Roles.Mistr, StringComparison.OrdinalIgnoreCase);

    public string CurrentUserLabel =>
        _appState.CurrentUser is null
            ? "Nepřihlášený uživatel"
            : $"Přihlášen: {_appState.CurrentUser.Username} ({GetRoleDisplayName(_appState.CurrentUser.Role)})";

    public bool IsAdmin => _appState.IsAdmin;

    public Visibility AdminSectionVisibility =>
        _appState.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

    private void OnCurrentProjectChanged(object? sender, ProjectInfo? project)
    {
        UpdateFromProject(project);
    }

    private void OnProjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is ObservableCollection<ProjectInfo> projects)
        {
            UpdateProjectStats(projects);
        }
    }

    private void OnDateChanged(object? sender, DateOnly newDate)
    {
        OnPropertyChanged(nameof(CurrentDate));
    }

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentUserLabel));
        OnPropertyChanged(nameof(IsAdmin));
        OnPropertyChanged(nameof(AdminSectionVisibility));
    }

    private void UpdateFromProject(ProjectInfo? project)
    {
        CurrentProjectName = project?.DisplayName ?? "(zatím nevybrán)";
        OnPropertyChanged(nameof(CurrentProjectCode));
        OnPropertyChanged(nameof(CurrentProjectOnlyName));
        OnPropertyChanged(nameof(CurrentProjectStatus));
        OnPropertyChanged(nameof(CurrentProjectStatusName));
    }

    private static string GetRoleDisplayName(string? role)
    {
        return Roles.GetDisplayName(role);
    }

    private void UpdateProjectStats(ObservableCollection<ProjectInfo> projects)
    {
        TotalProjects = projects.Count;
        PreparationCount = projects.Count(p => p.Status == ProjectStatus.Preparation);
        ExecutionCount = projects.Count(p => p.Status == ProjectStatus.Execution);
        CompletedCount = projects.Count(p => p.Status == ProjectStatus.Completed);
    }
}
