using System;
using System.Windows.Input;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public class ProjectDetailPageViewModel : ViewModelBase
{
    private readonly IAppStateService _appState;
    private ProjectInfo? _currentProject;
    private bool _isInfoPaneOpen = true;

    public ProjectDetailPageViewModel(IAppStateService appState)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _currentProject = _appState.CurrentProject;
        _appState.CurrentProjectChanged += OnCurrentProjectChanged;
        ToggleInfoPaneCommand = new RelayCommand(_ => IsInfoPaneOpen = !IsInfoPaneOpen);
    }

    public bool CanEditProject =>
        string.Equals(_appState.CurrentUser?.Role, Roles.Technik, StringComparison.OrdinalIgnoreCase);

    public ProjectInfo? CurrentProject
    {
        get => _currentProject;
        private set
        {
            if (SetProperty(ref _currentProject, value))
            {
                OnPropertyChanged(nameof(ProjectDisplayName));
                OnPropertyChanged(nameof(ProjectCode));
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(ProjectStatus));
            }
        }
    }

    public string ProjectDisplayName => CurrentProject?.DisplayName ?? "(bez názvu)";

    public string ProjectCode => CurrentProject?.ProjectCode ?? "-";

    public string ProjectName => CurrentProject?.ProjectName ?? "-";

    public string ProjectStatus => CurrentProject?.Status.ToString() ?? "N/A";

    public bool IsInfoPaneOpen
    {
        get => _isInfoPaneOpen;
        set => SetProperty(ref _isInfoPaneOpen, value);
    }

    public ICommand ToggleInfoPaneCommand { get; }

    private void OnCurrentProjectChanged(object? sender, ProjectInfo? project)
    {
        CurrentProject = project;
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
