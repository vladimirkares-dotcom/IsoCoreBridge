using System;
using IsoCore.App.State;
using IsoCore.Domain;

namespace IsoCore.App.Services;

public interface IAppStateService
{
    ProjectInfo? CurrentProject { get; }
    DateOnly CurrentDate { get; }
    ProjectRegistry ProjectRegistry { get; }
    BuildingObjectInfo? CurrentBuildingObject { get; }
    UserAccount? CurrentUser { get; }
    bool IsAdmin { get; }
    string? PendingPasswordChangeUsername { get; }

    event EventHandler<ProjectInfo?>? CurrentProjectChanged;
    event EventHandler<DateOnly>? CurrentDateChanged;
    event EventHandler<BuildingObjectInfo?>? CurrentBuildingObjectChanged;
    event EventHandler? CurrentUserChanged;

    void SetCurrentProject(ProjectInfo? project);
    void SetCurrentDate(DateOnly date);
    void SetCurrentBuildingObject(BuildingObjectInfo? obj);
    void SetCurrentUser(UserAccount? user);
    void Logout();
    void RequestPasswordChange(string username);
    void ClearPasswordChangeRequest();
}

public class AppStateService : IAppStateService
{
    private readonly ProjectRegistry _projectRegistry;
    private ProjectInfo? _currentProject;
    private DateOnly _currentDate = DateOnly.FromDateTime(DateTime.Today);
    private BuildingObjectInfo? _currentBuildingObject;
    private UserAccount? _currentUser;
    private string? _pendingPasswordChangeUsername;

    public AppStateService(ProjectRegistry projectRegistry)
    {
        _projectRegistry = projectRegistry ?? throw new ArgumentNullException(nameof(projectRegistry));
    }

    public ProjectInfo? CurrentProject => _currentProject;
    public DateOnly CurrentDate => _currentDate;
    public ProjectRegistry ProjectRegistry => _projectRegistry;
    public BuildingObjectInfo? CurrentBuildingObject => _currentBuildingObject;
    public UserAccount? CurrentUser => _currentUser;
    public bool IsAdmin => string.Equals(_currentUser?.Role, Roles.Administrator, StringComparison.OrdinalIgnoreCase);

    public event EventHandler<ProjectInfo?>? CurrentProjectChanged;
    public event EventHandler<DateOnly>? CurrentDateChanged;
    public event EventHandler<BuildingObjectInfo?>? CurrentBuildingObjectChanged;
    public event EventHandler? CurrentUserChanged;

    public void SetCurrentProject(ProjectInfo? project)
    {
        if (!Equals(_currentProject, project))
        {
            _currentProject = project;
            CurrentProjectChanged?.Invoke(this, _currentProject);
        }
    }

    public void SetCurrentDate(DateOnly date)
    {
        if (_currentDate != date)
        {
            _currentDate = date;
            CurrentDateChanged?.Invoke(this, _currentDate);
        }
    }

    public void SetCurrentBuildingObject(BuildingObjectInfo? obj)
    {
        if (!Equals(_currentBuildingObject, obj))
        {
            _currentBuildingObject = obj;
            CurrentBuildingObjectChanged?.Invoke(this, _currentBuildingObject);
        }
    }

    public void SetCurrentUser(UserAccount? user)
    {
        if (_currentUser?.Id == user?.Id)
        {
            return;
        }

        _currentUser = user;
        CurrentUserChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Logout() => SetCurrentUser(null);

    public string? PendingPasswordChangeUsername => _pendingPasswordChangeUsername;

    public void RequestPasswordChange(string username)
    {
        _pendingPasswordChangeUsername = username;
    }

    public void ClearPasswordChangeRequest()
    {
        _pendingPasswordChangeUsername = null;
    }
}
