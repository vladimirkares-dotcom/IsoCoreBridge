using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IsoCore.App.Services;
using IsoCore.App.State;
using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public class ProjectsViewModel : ViewModelBase
{
    private readonly IAppStateService _appState;
    private bool _isBusy;
    private ProjectInfo? _selectedProject;

    public ProjectsViewModel(IAppStateService appState)
    {
        _appState = appState;
        Projects = _appState.ProjectRegistry?.Projects ?? new ObservableCollection<ProjectInfo>();
        _appState.CurrentProjectChanged += OnCurrentProjectChanged;
    }

    public ObservableCollection<ProjectInfo> Projects { get; }

    public ProjectInfo? CurrentProject => _appState.CurrentProject;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public ProjectInfo? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                _appState.SetCurrentProject(value);
            }
        }
    }

    public async Task LoadProjectsAsync()
    {
        if (_appState.ProjectRegistry == null)
        {
            return;
        }

        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _appState.ProjectRegistry.LoadFromStorageAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void SetCurrentProject(ProjectInfo? project) => _appState.SetCurrentProject(project);

    public void OpenProject(ProjectInfo project) => SetCurrentProject(project);

    public void ClearCurrentProject() => SetCurrentProject(null);

    private void OnCurrentProjectChanged(object? sender, ProjectInfo? project)
    {
        if (!Equals(_selectedProject, project))
        {
            _selectedProject = project;
            OnPropertyChanged(nameof(SelectedProject));
        }
    }
}
