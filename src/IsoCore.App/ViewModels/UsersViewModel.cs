using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Login { get; set; } = string.Empty;
    public string WorkerNumber { get; set; } = string.Empty;
    public string TitleBefore { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string TitleAfter { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    public string RoleDisplayName => Roles.GetDisplayName(Role);
    public string Status => IsActive ? "Aktivní" : "Neaktivní";
}

public class UsersViewModel : ViewModelBase
{
    private readonly IUserAuthService _authService;
    private readonly IAppStateService _appState;

    private UserListItem? _selectedUser;
    private string _editUsername = string.Empty;
    private string _editDisplayName = string.Empty;
    private string _editRole = string.Empty;
    private string _editLogin = string.Empty;
    private string _editWorkerNumber = string.Empty;
    private string _editTitleBefore = string.Empty;
    private string _editFirstName = string.Empty;
    private string _editLastName = string.Empty;
    private string _editTitleAfter = string.Empty;
    private string _editJobTitle = string.Empty;
    private string _editCompanyName = string.Empty;
    private string _editCompanyAddress = string.Empty;
    private string _editPhoneNumber = string.Empty;
    private string _editNote = string.Empty;
    private bool _editIsActive = true;
    private bool _isBusy;

    public Microsoft.UI.Xaml.Visibility AdminSectionVisibility => _appState.IsAdmin ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    private bool _isNewUser;
    private string _userFormError = string.Empty;

    public ObservableCollection<UserListItem> Users { get; } = new();

    public IReadOnlyList<RoleOption> RoleOptions { get; } =
        Roles.All.Select(role => new RoleOption(role, Roles.GetDisplayName(role))).ToList();

    public UserListItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                if (value == null)
                {
                    EditUsername = string.Empty;
                    EditDisplayName = string.Empty;
                    EditRole = string.Empty;
                    EditLogin = string.Empty;
                    EditWorkerNumber = string.Empty;
                    EditTitleBefore = string.Empty;
                    EditFirstName = string.Empty;
                    EditLastName = string.Empty;
                    EditTitleAfter = string.Empty;
                    EditJobTitle = string.Empty;
                    EditCompanyName = string.Empty;
                    EditCompanyAddress = string.Empty;
                    EditPhoneNumber = string.Empty;
                    EditNote = string.Empty;
                    EditIsActive = true;
                }
                else
                {
                    EditUsername = value.Username ?? string.Empty;
                    EditDisplayName = value.DisplayName ?? string.Empty;
                    EditRole = value.Role ?? string.Empty;
                    EditLogin = value.Login ?? string.Empty;
                    EditWorkerNumber = value.WorkerNumber ?? string.Empty;
                    EditTitleBefore = value.TitleBefore ?? string.Empty;
                    EditFirstName = value.FirstName ?? string.Empty;
                    EditLastName = value.LastName ?? string.Empty;
                    EditTitleAfter = value.TitleAfter ?? string.Empty;
                    EditJobTitle = value.JobTitle ?? string.Empty;
                    EditCompanyName = value.CompanyName ?? string.Empty;
                    EditCompanyAddress = value.CompanyAddress ?? string.Empty;
                    EditPhoneNumber = value.PhoneNumber ?? string.Empty;
                    EditNote = value.Note ?? string.Empty;
                    EditIsActive = value.IsActive;
                }
                UserFormError = string.Empty;
            }
        }
    }

    public string EditUsername
    {
        get => _editUsername;
        set => SetProperty(ref _editUsername, value);
    }

    public string EditDisplayName
    {
        get => _editDisplayName;
        set => SetProperty(ref _editDisplayName, value);
    }

    public string EditRole
    {
        get => _editRole;
        set => SetProperty(ref _editRole, value);
    }

    public string EditLogin
    {
        get => _editLogin;
        set => SetProperty(ref _editLogin, value);
    }

    public string EditWorkerNumber
    {
        get => _editWorkerNumber;
        set => SetProperty(ref _editWorkerNumber, value);
    }

    public string EditTitleBefore
    {
        get => _editTitleBefore;
        set => SetProperty(ref _editTitleBefore, value);
    }

    public string EditFirstName
    {
        get => _editFirstName;
        set => SetProperty(ref _editFirstName, value);
    }

    public string EditLastName
    {
        get => _editLastName;
        set => SetProperty(ref _editLastName, value);
    }

    public string EditTitleAfter
    {
        get => _editTitleAfter;
        set => SetProperty(ref _editTitleAfter, value);
    }

    public string EditJobTitle
    {
        get => _editJobTitle;
        set => SetProperty(ref _editJobTitle, value);
    }

    public string EditCompanyName
    {
        get => _editCompanyName;
        set => SetProperty(ref _editCompanyName, value);
    }

    public string EditCompanyAddress
    {
        get => _editCompanyAddress;
        set => SetProperty(ref _editCompanyAddress, value);
    }

    public string EditPhoneNumber
    {
        get => _editPhoneNumber;
        set => SetProperty(ref _editPhoneNumber, value);
    }

    public bool EditIsActive
    {
        get => _editIsActive;
        set => SetProperty(ref _editIsActive, value);
    }

    public string EditNote
    {
        get => _editNote;
        set => SetProperty(ref _editNote, value);
    }

    public bool IsNewUser
    {
        get => _isNewUser;
        set => SetProperty(ref _isNewUser, value);
    }

    public string UserFormError
    {
        get => _userFormError;
        set => SetProperty(ref _userFormError, value);
    }

    public string CurrentUserLabel
    {
        get
        {
            var user = _appState.CurrentUser;
            if (user == null)
            {
                return "Nepřihlášený uživatel";
            }

            var name = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
            var roleName = Roles.GetDisplayName(user.Role);
            return $"Přihlášen: {name} ({roleName})";
        }
    }

    public UsersViewModel(IUserAuthService authService, IAppStateService appState)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        ReloadUsersFromService();
        StartNewUser();
        _appState.CurrentUserChanged += OnCurrentUserChanged;
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public async Task LoadUsersAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            Users.Clear();

            var allUsers = await App.UserAuthService.GetUsersAsync().ConfigureAwait(false);
            foreach (var u in allUsers)
            {
                Users.Add(new UserListItem
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName,
                    Role = u.Role,
                    Login = u.Login,
                    WorkerNumber = u.WorkerNumber,
                    TitleBefore = u.TitleBefore,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    TitleAfter = u.TitleAfter,
                    JobTitle = u.JobTitle,
                    CompanyName = u.CompanyName,
                    CompanyAddress = u.CompanyAddress,
                    PhoneNumber = u.PhoneNumber,
                    Note = u.Note,
                    IsActive = u.IsActive
                });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveUserAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (SelectedUser == null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var user = new UserAccount
            {
                Id = SelectedUser.Id,
                Username = SelectedUser.Username,
                DisplayName = SelectedUser.DisplayName,
                Role = SelectedUser.Role,
                Login = SelectedUser.Login,
                WorkerNumber = SelectedUser.WorkerNumber,
                TitleBefore = SelectedUser.TitleBefore,
                FirstName = SelectedUser.FirstName,
                LastName = SelectedUser.LastName,
                TitleAfter = SelectedUser.TitleAfter,
                JobTitle = SelectedUser.JobTitle,
                CompanyName = SelectedUser.CompanyName,
                CompanyAddress = SelectedUser.CompanyAddress,
                PhoneNumber = SelectedUser.PhoneNumber,
                Note = SelectedUser.Note,
                IsActive = SelectedUser.IsActive
            };

            await App.UserAuthService.UpdateUserAsync(user.Username, user).ConfigureAwait(false);
            await LoadUsersAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeleteUserAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (SelectedUser == null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await App.UserAuthService.DeleteUserAsync(SelectedUser.Username).ConfigureAwait(false);
            SelectedUser = null;
            await LoadUsersAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task CreateUserAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var user = new UserAccount
            {
                Id = string.Empty,
                Username = string.IsNullOrWhiteSpace(EditUsername) ? string.Empty : EditUsername.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(EditDisplayName) ? EditUsername.Trim() : EditDisplayName.Trim(),
                Role = string.IsNullOrWhiteSpace(EditRole) ? Roles.Technik : EditRole.Trim(),
                Login = string.IsNullOrWhiteSpace(EditLogin) ? EditUsername.Trim() : EditLogin.Trim(),
                WorkerNumber = EditWorkerNumber.Trim(),
                TitleBefore = EditTitleBefore.Trim(),
                FirstName = EditFirstName.Trim(),
                LastName = EditLastName.Trim(),
                TitleAfter = EditTitleAfter.Trim(),
                JobTitle = EditJobTitle.Trim(),
                CompanyName = EditCompanyName.Trim(),
                CompanyAddress = EditCompanyAddress.Trim(),
                PhoneNumber = EditPhoneNumber.Trim(),
                Note = EditNote.Trim(),
                IsActive = EditIsActive
            };

            await App.UserAuthService.CreateUserAsync(user).ConfigureAwait(false);
            await LoadUsersAsync().ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadSelectedUserIntoForm()
    {
        if (SelectedUser == null)
        {
            EditUsername = string.Empty;
            EditDisplayName = string.Empty;
            EditRole = Roles.Technik;
            EditLogin = string.Empty;
            EditWorkerNumber = string.Empty;
            EditTitleBefore = string.Empty;
            EditFirstName = string.Empty;
            EditLastName = string.Empty;
            EditTitleAfter = string.Empty;
            EditJobTitle = string.Empty;
            EditCompanyName = string.Empty;
            EditCompanyAddress = string.Empty;
            EditPhoneNumber = string.Empty;
            EditNote = string.Empty;
            EditIsActive = true;
            IsNewUser = true;
            UserFormError = string.Empty;
        }
        else
        {
            EditUsername = SelectedUser.Username;
            EditDisplayName = SelectedUser.DisplayName;
            EditRole = SelectedUser.Role;
            EditLogin = string.IsNullOrWhiteSpace(SelectedUser.Login) ? SelectedUser.Username : SelectedUser.Login;
            EditWorkerNumber = SelectedUser.WorkerNumber;
            EditTitleBefore = SelectedUser.TitleBefore;
            EditFirstName = SelectedUser.FirstName;
            EditLastName = SelectedUser.LastName;
            EditTitleAfter = SelectedUser.TitleAfter;
            EditJobTitle = SelectedUser.JobTitle;
            EditCompanyName = SelectedUser.CompanyName;
            EditCompanyAddress = SelectedUser.CompanyAddress;
            EditPhoneNumber = SelectedUser.PhoneNumber;
            EditNote = SelectedUser.Note;
            EditIsActive = SelectedUser.IsActive;
            IsNewUser = false;
            UserFormError = string.Empty;
        }
    }

    public void StartNewUser()
    {
        SelectedUser = null;
        EditUsername = string.Empty;
        EditDisplayName = string.Empty;
        EditRole = Roles.Technik;
        EditLogin = string.Empty;
        EditWorkerNumber = string.Empty;
        EditTitleBefore = string.Empty;
        EditFirstName = string.Empty;
        EditLastName = string.Empty;
        EditTitleAfter = string.Empty;
        EditJobTitle = string.Empty;
        EditCompanyName = string.Empty;
        EditCompanyAddress = string.Empty;
        EditPhoneNumber = string.Empty;
        EditIsActive = true;
        EditNote = string.Empty;
        IsNewUser = true;
        UserFormError = string.Empty;
    }

    public async Task<bool> SaveCurrentUserAsync(string? newPassword, string? confirmPassword)
    {
        UserFormError = string.Empty;

        if (string.IsNullOrWhiteSpace(EditUsername))
        {
            UserFormError = "Uživatelské jméno nesmí být prázdné.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmPassword))
        {
            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                UserFormError = "Hesla se neshodují.";
                return false;
            }

            if (!string.IsNullOrEmpty(newPassword) && newPassword.Length < 6)
            {
                UserFormError = "Heslo musí mít alespoň 6 znaků.";
                return false;
            }
        }

        var passwordProvided = !string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmPassword);

        var user = new UserAccount
        {
            Id = IsNewUser ? string.Empty : SelectedUser?.Id ?? string.Empty,
            Username = EditUsername.Trim(),
            DisplayName = EditDisplayName.Trim(),
            Role = string.IsNullOrWhiteSpace(EditRole) ? Roles.Technik : EditRole.Trim(),
            Login = string.IsNullOrWhiteSpace(EditLogin) ? EditUsername.Trim() : EditLogin.Trim(),
            WorkerNumber = EditWorkerNumber.Trim(),
            TitleBefore = EditTitleBefore.Trim(),
            FirstName = EditFirstName.Trim(),
            LastName = EditLastName.Trim(),
            TitleAfter = EditTitleAfter.Trim(),
            JobTitle = EditJobTitle.Trim(),
            CompanyName = EditCompanyName.Trim(),
            CompanyAddress = EditCompanyAddress.Trim(),
            PhoneNumber = EditPhoneNumber.Trim(),
            Note = EditNote.Trim(),
            IsActive = EditIsActive
        };

        var saved = await _authService.CreateOrUpdateUserAsync(user, plainPassword: null);

        if (passwordProvided)
        {
            await _authService.SetPasswordAsync(saved.Username, newPassword!);
        }

        ReloadUsersFromService(saved.Username);
        return true;
    }

    public async Task ToggleActiveForSelectedAsync()
    {
        if (SelectedUser == null)
        {
            return;
        }

        var newIsActive = !SelectedUser.IsActive;
        await _authService.ToggleActiveAsync(SelectedUser.Id, newIsActive);
        ReloadUsersFromService(SelectedUser.Username);
    }

    public async Task DeleteSelectedUserAsync()
    {
        if (SelectedUser == null)
        {
            return;
        }

        await _authService.DeleteUserAsync(SelectedUser.Username);
        await ReloadUsersAsync().ConfigureAwait(false);
    }

    public void DeleteSelectedUserInMemory()
    {
        if (SelectedUser == null)
        {
            return;
        }

        Users.Remove(SelectedUser);
    }

    public async void ToggleActiveForSelected()
    {
        await ToggleActiveForSelectedAsync();
    }

    private Task ReloadUsersAsync(string? usernameToSelect = null)
    {
        ReloadUsersFromService(usernameToSelect);
        return Task.CompletedTask;
    }

    private void ReloadUsersFromService(string? usernameToSelect = null)
    {
        Users.Clear();

        var allUsers = _authService.GetAllUsers()
            .OrderBy(u => u.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        UserListItem? newlySelected = null;

        foreach (var u in allUsers)
        {
            var item = new UserListItem
            {
                Id = u.Id,
                Username = u.Username,
                DisplayName = u.DisplayName,
                Role = u.Role,
                Login = u.Login,
                WorkerNumber = u.WorkerNumber,
                TitleBefore = u.TitleBefore,
                FirstName = u.FirstName,
                LastName = u.LastName,
                TitleAfter = u.TitleAfter,
                JobTitle = u.JobTitle,
                CompanyName = u.CompanyName,
                CompanyAddress = u.CompanyAddress,
                PhoneNumber = u.PhoneNumber,
                Note = u.Note,
                IsActive = u.IsActive
            };

            Users.Add(item);

            if (!string.IsNullOrEmpty(usernameToSelect) &&
                string.Equals(u.Username, usernameToSelect, StringComparison.OrdinalIgnoreCase))
            {
                newlySelected = item;
            }
        }

        SelectedUser = newlySelected;
        if (newlySelected == null)
        {
            StartNewUser();
        }
    }

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentUserLabel));
        OnPropertyChanged(nameof(AdminSectionVisibility));
    }
}
