using System;
using System.Threading.Tasks;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public sealed class SettingsPageViewModel : ViewModelBase
{
    private readonly IUserAuthService _authService;
    private bool _canEditSettings;
    private string _currentUsername = string.Empty;
    private string _currentDisplayName = string.Empty;
    private string _currentRoleName = string.Empty;
    private string _currentLogin = string.Empty;

    public bool CanEditSettings
    {
        get => _canEditSettings;
        private set => SetProperty(ref _canEditSettings, value);
    }

    public string CurrentUsername
    {
        get => _currentUsername;
        private set => SetProperty(ref _currentUsername, value);
    }

    public string CurrentDisplayName
    {
        get => _currentDisplayName;
        private set => SetProperty(ref _currentDisplayName, value);
    }

    public string CurrentRoleName
    {
        get => _currentRoleName;
        private set => SetProperty(ref _currentRoleName, value);
    }

    public string CurrentLogin
    {
        get => _currentLogin;
        private set => SetProperty(ref _currentLogin, value);
    }

    public SettingsPageViewModel(IUserAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        RefreshFromAppState();
    }

    public async Task InitializeAsync()
    {
        CanEditSettings = await _authService.IsInRoleAsync(Roles.Administrator);
    }

    public void RefreshFromAppState()
    {
        var user = App.AppState.CurrentUser;

        if (user == null)
        {
            CurrentUsername = string.Empty;
            CurrentDisplayName = string.Empty;
            CurrentRoleName = string.Empty;
            CurrentLogin = string.Empty;
            return;
        }

        CurrentUsername = user.Username ?? string.Empty;
        CurrentDisplayName = user.DisplayName ?? string.Empty;
        CurrentRoleName = Roles.GetDisplayName(user.Role);
        CurrentLogin = user.Login ?? string.Empty;
    }
}
