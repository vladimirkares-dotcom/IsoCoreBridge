using System;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
                OnPropertyChanged(nameof(HasSelectedProject));
            }
        }
    }

    public bool HasSelectedProject => SelectedProject != null;

    public async Task LoadProjectsAsync()
    {
        if (_appState.ProjectRegistry == null || IsBusy)
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

    public async Task CreateAndAddProjectAsync(string code, string name)
    {
        if (_appState.ProjectRegistry == null || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var normalizedCode = string.IsNullOrWhiteSpace(code) ? Guid.NewGuid().ToString("N") : code;

        if (_appState.ProjectRegistry.FindByCode(normalizedCode) != null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        var project = new ProjectInfo
        {
            Id = Guid.NewGuid().ToString(),
            Code = normalizedCode,
            Name = name,
            Description = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            Status = ProjectStatus.Preparation
        };

        await _appState.ProjectRegistry.AddProjectAsync(project);

        SelectedProject = project;
        SetCurrentProject(project);
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

    public async Task<bool> DeleteSelectedProjectAsync()
    {
        if (SelectedProject == null)
        {
            return false;
        }

        var registry = _appState.ProjectRegistry;
        if (registry == null)
        {
            return false;
        }

        var target = registry.Projects.FirstOrDefault(p => ReferenceEquals(p, SelectedProject))
                     ?? registry.Projects.FirstOrDefault(p => string.Equals(p.Id, SelectedProject.Id, StringComparison.OrdinalIgnoreCase))
                     ?? registry.Projects.FirstOrDefault(p => string.Equals(p.Code, SelectedProject.Code, StringComparison.OrdinalIgnoreCase));

        if (target == null)
        {
            return false;
        }

        await registry.DeleteProjectAsync(target);

        if (ReferenceEquals(_appState.CurrentProject, target))
        {
            _appState.SetCurrentProject(null);
        }

        SelectedProject = null;
        return true;
    }

    public async Task<bool> UpdateSelectedProjectAsync(string newCode, string newName)
    {
        if (SelectedProject == null || string.IsNullOrWhiteSpace(newCode) || string.IsNullOrWhiteSpace(newName))
        {
            return false;
        }

        var registry = _appState.ProjectRegistry;
        if (registry == null)
        {
            return false;
        }

        var duplicate = registry.Projects.Any(p =>
            !ReferenceEquals(p, SelectedProject) &&
            string.Equals(p.Code, newCode, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            return false;
        }

        SelectedProject.Code = newCode;
        SelectedProject.Name = newName;
        SelectedProject.UpdatedAt = DateTime.UtcNow;

        await registry.UpdateProjectAsync(SelectedProject);

        SetCurrentProject(SelectedProject);
        return true;
    }
}
