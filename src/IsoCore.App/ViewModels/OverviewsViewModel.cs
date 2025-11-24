using System;
using System.Globalization;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public class OverviewsViewModel : ViewModelBase, IDisposable
{
    private readonly IAppStateService _appState;
    private ProjectInfo? _currentProject;
    private DateOnly _currentDate;
    private bool _disposed;

    public OverviewsViewModel(IAppStateService appState)
    {
        _appState = appState;
        _currentProject = _appState.CurrentProject;
        _currentDate = _appState.CurrentDate;

        _appState.CurrentProjectChanged += OnCurrentProjectChanged;
        _appState.CurrentDateChanged += OnCurrentDateChanged;
    }

    public ProjectInfo? CurrentProject => _currentProject;

    public DateOnly CurrentDate => _currentDate;

    public string CurrentProjectDisplayName =>
        _currentProject?.DisplayName ?? _currentProject?.ProjectName ?? "(žádný projekt)";

    public string CurrentDateText => _currentDate.ToString("d", CultureInfo.CurrentCulture);

    public event EventHandler<ProjectInfo?>? CurrentProjectChanged;
    public event EventHandler<DateOnly>? CurrentDateChanged;

    public void SetCurrentProject(ProjectInfo? project) => _appState.SetCurrentProject(project);

    public void SetCurrentDate(DateOnly date) => _appState.SetCurrentDate(date);

    private void OnCurrentProjectChanged(object? sender, ProjectInfo? project)
    {
        _currentProject = project;
        OnPropertyChanged(nameof(CurrentProject));
        OnPropertyChanged(nameof(CurrentProjectDisplayName));
        CurrentProjectChanged?.Invoke(this, project);
    }

    private void OnCurrentDateChanged(object? sender, DateOnly date)
    {
        _currentDate = date;
        OnPropertyChanged(nameof(CurrentDate));
        OnPropertyChanged(nameof(CurrentDateText));
        CurrentDateChanged?.Invoke(this, date);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _appState.CurrentProjectChanged -= OnCurrentProjectChanged;
        _appState.CurrentDateChanged -= OnCurrentDateChanged;
        _disposed = true;
    }
}
