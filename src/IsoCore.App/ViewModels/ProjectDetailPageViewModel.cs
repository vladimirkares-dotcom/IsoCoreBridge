using System;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public class ProjectDetailPageViewModel : ViewModelBase
{
    private readonly IAppStateService _appState;

    public ProjectDetailPageViewModel(IAppStateService appState)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
    }

    public bool CanEditProject =>
        string.Equals(_appState.CurrentUser?.Role, Roles.Technik, StringComparison.OrdinalIgnoreCase);
}
