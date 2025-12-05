using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using IsoCore.App.Services.Auth;
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
    public event EventHandler<bool>? UserSaved;

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
    private string _userFormSuccess = string.Empty;
    private readonly Dictionary<string, string> _displayToDomainRoleMap;
    private readonly Dictionary<string, string> _domainToDisplayRoleMap;
    private string? _currentEditUserId;
    private string? _currentEditUserLogin;

    public ObservableCollection<UserListItem> Users { get; } = new();

    public IReadOnlyList<string> AvailableRoles { get; } =
        new[] { "Administrátor", "Mistr", "Kontrolor", "Technik", "Předák", "Dělník" };

    public IReadOnlyList<RoleOption> RoleOptions { get; }

    public ICommand NewUserCommand { get; }
    public ICommand SaveUserCommand { get; }
    public ICommand DeleteUserCommand { get; }

    public UserListItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                OnSelectedUserChanged(value);
            }
        }
    }

    public string EditUsername
    {
        get => _editUsername;
        set
        {
            if (SetProperty(ref _editUsername, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string EditDisplayName
    {
        get => _editDisplayName;
        set
        {
            if (SetProperty(ref _editDisplayName, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string EditRole
    {
        get => _editRole;
        set
        {
            var normalizedRole = NormalizeRoleForEdit(value);
            if (SetProperty(ref _editRole, normalizedRole))
            {
                UpdateCommandStates();
            }
        }
    }

    public string EditLogin
    {
        get => _editLogin;
        set
        {
            if (SetProperty(ref _editLogin, value))
            {
                UpdateCommandStates();
            }
        }
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
        set
        {
            if (SetProperty(ref _editFirstName, value))
            {
                AutoGenerateLoginForNewUser();
            }
        }
    }

    public string EditLastName
    {
        get => _editLastName;
        set
        {
            if (SetProperty(ref _editLastName, value))
            {
                AutoGenerateLoginForNewUser();
            }
        }
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
        set
        {
            if (SetProperty(ref _editCompanyName, value))
            {
                OnPropertyChanged(nameof(EmploymentType));
            }
        }
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

    public string UserFormSuccess
    {
        get => _userFormSuccess;
        set => SetProperty(ref _userFormSuccess, value);
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

    public string EmploymentType =>
        IsCoreCompany(EditCompanyName) ? "Kmenov� zam�stnanec" : "Subdodavatel";

    public UsersViewModel(IUserAuthService authService, IAppStateService appState)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        RoleOptions = AvailableRoles.Select(role => new RoleOption(role, role)).ToList();
        _displayToDomainRoleMap = BuildDisplayToDomainRoleMap();
        _domainToDisplayRoleMap = BuildDomainToDisplayRoleMap();

        NewUserCommand = new RelayCommand(_ => StartCreateUser(), _ => !IsBusy);
        SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync().ConfigureAwait(false), _ => CanExecuteSaveUser());
        DeleteUserCommand = new RelayCommand(async _ => await DeleteSelectedUserAsync().ConfigureAwait(false), _ => CanExecuteDeleteUser());

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
            // This method is called from a UI event handler.
            // The collection modifications must happen on the UI thread.
            // The I/O operation should be offloaded.
            // The previous implementation was doing I/O and then modifying the collection
            // on a background thread, which is incorrect.

            var allUsers = await App.UserAuthService.GetUsersAsync().ConfigureAwait(false);

            // Marshal collection updates to the UI thread
            var dispatcher = App.MainDispatcherQueue;
            if (dispatcher != null)
            {
                dispatcher.TryEnqueue(() =>
                {
                    Users.Clear();
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
                });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [Obsolete("This method has unsafe threading. Use SaveCurrentUserAsync instead.")]
    public async Task SaveUserAsyncLegacy()
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

    [Obsolete("This method has unsafe threading. Use DeleteSelectedUserAsync instead.")]
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

    [Obsolete("This method has unsafe threading. Use SaveCurrentUserAsync instead.")]
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
            ResetEditFieldsForNewUser();
        }
        else
        {
            EditUsername = SelectedUser.Username;
            EditDisplayName = SelectedUser.DisplayName;
            EditRole = NormalizeRoleForEdit(SelectedUser.Role);
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
            UserFormSuccess = string.Empty;
            _currentEditUserId = SelectedUser.Id;
            _currentEditUserLogin = string.IsNullOrWhiteSpace(SelectedUser.Login) ? SelectedUser.Username : SelectedUser.Login;
        }
    }

    public void StartNewUser()
    {
        ResetEditFieldsForNewUser();
        SelectedUser = null;
    }

    public void StartCreateUser()
    {
        ResetEditFieldsForNewUser();
        SelectedUser = null;
        IsNewUser = true;
        UserFormError = string.Empty;
        UserFormSuccess = string.Empty;
        _currentEditUserId = null;
        _currentEditUserLogin = null;
    }

    public void StartEditSelectedUser()
    {
        if (SelectedUser == null)
        {
            SetError("Vyberte uživatele ze seznamu.");
            return;
        }

        IsNewUser = false;
        LoadSelectedUserIntoForm();
        _currentEditUserId = SelectedUser.Id;
        _currentEditUserLogin = string.IsNullOrWhiteSpace(SelectedUser.Login) ? SelectedUser.Username : SelectedUser.Login;
        UserFormError = string.Empty;
        UserFormSuccess = string.Empty;
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
            Role = NormalizeRoleForDomain(EditRole),
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
        SetSuccess(newIsActive ? "Uživatel byl aktivován." : "Uživatel byl deaktivován.");
    }

    public async Task DeleteSelectedUserAsync()
    {
        if (SelectedUser == null)
        {
            return;
        }

        SetError(string.Empty);
        SetSuccess(string.Empty);

        var username = SelectedUser.Username;
        var success = false;

        try
        {
            await _authService.DeleteUserAsync(username).ConfigureAwait(false);
            success = true;
        }
        catch
        {
            SetError("Smazání uživatele se nezdařilo.");
        }

        if (!success)
        {
            return;
        }

        await LoadUsersAsync().ConfigureAwait(false);
        StartNewUser();
        SetSuccess("Uživatel byl smazán.");
    }

    public async void ToggleActiveForSelected()
    {
        await ToggleActiveForSelectedAsync();
    }

    private Task ReloadUsersAsync(string? usernameToSelect = null, string? userIdToSelect = null)
    {
        ReloadUsersFromService(usernameToSelect, userIdToSelect);
        return Task.CompletedTask;
    }

    private void ReloadUsersFromService(string? usernameToSelect = null, string? userIdToSelect = null)
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

            if (!string.IsNullOrEmpty(userIdToSelect) &&
                string.Equals(u.Id, userIdToSelect, StringComparison.OrdinalIgnoreCase))
            {
                newlySelected = item;
            }
            else if (!string.IsNullOrEmpty(usernameToSelect) &&
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

    private void OnSelectedUserChanged(UserListItem? value)
    {
        if (value == null)
        {
            ResetEditFieldsForNewUser();
        }
        else
        {
            LoadSelectedUserIntoForm();
            IsNewUser = false;
            UserFormError = string.Empty;
        }

        UpdateCommandStates();
    }

    private void ResetEditFieldsForNewUser()
    {
        ClearEditFields();
        IsNewUser = true;
        UserFormError = string.Empty;
        UserFormSuccess = string.Empty;
        _currentEditUserId = null;
        _currentEditUserLogin = null;
        UpdateCommandStates();
    }

    private void ClearEditFields()
    {
        EditUsername = string.Empty;
        EditDisplayName = string.Empty;
        EditRole = AvailableRoles.First();
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

    private string NormalizeRoleForEdit(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return AvailableRoles.First();
        }

        if (_domainToDisplayRoleMap.TryGetValue(role.Trim(), out var displayRole))
        {
            return displayRole;
        }

        var match = AvailableRoles.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        return match ?? AvailableRoles.First();
    }

    private string NormalizeRoleForDomain(string? roleDisplay)
    {
        if (string.IsNullOrWhiteSpace(roleDisplay))
        {
            return Roles.Technik;
        }

        if (_displayToDomainRoleMap.TryGetValue(roleDisplay.Trim(), out var domainRole))
        {
            return domainRole;
        }

        return Roles.Technik;
    }

    private Dictionary<string, string> BuildDisplayToDomainRoleMap()
    {
        var domainRoles = new[] { Roles.Kontrolor, Roles.Administrator, Roles.Mistr, Roles.Technik, Roles.Predak, Roles.Delnik };

        return AvailableRoles
            .Zip(domainRoles, (display, domain) => new { display, domain })
            .ToDictionary(p => p.display, p => p.domain, StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, string> BuildDomainToDisplayRoleMap()
    {
        var domainRoles = new[] { Roles.Kontrolor, Roles.Administrator, Roles.Mistr, Roles.Technik, Roles.Predak, Roles.Delnik };

        return domainRoles
            .Zip(AvailableRoles, (domain, display) => new { domain, display })
            .ToDictionary(p => p.domain, p => p.display, StringComparer.OrdinalIgnoreCase);
    }

    private void AutoGenerateLoginForNewUser()
    {
        if (!IsNewUser)
        {
            return;
        }

        // Do not overwrite a login that the user already typed.
        if (!string.IsNullOrWhiteSpace(EditLogin))
        {
            return;
        }

        var first = (EditFirstName ?? string.Empty).Trim();
        var last = (EditLastName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
        {
            return;
        }

        var baseLogin = BuildBaseLoginFromName(first, last);
        if (string.IsNullOrWhiteSpace(baseLogin))
        {
            return;
        }

        var uniqueLogin = GenerateUniqueLogin(baseLogin);
        if (string.IsNullOrWhiteSpace(uniqueLogin))
        {
            return;
        }

        EditLogin = uniqueLogin;
    }

    private static string BuildBaseLoginFromName(string firstName, string lastName)
    {
        var first = RemoveDiacritics(firstName).ToLowerInvariant();
        var last = RemoveDiacritics(lastName).ToLowerInvariant();

        first = new string(first.Where(char.IsLetterOrDigit).ToArray());
        last = new string(last.Where(char.IsLetterOrDigit).ToArray());

        if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(last))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(last))
        {
            return first;
        }

        if (string.IsNullOrWhiteSpace(first))
        {
            return last;
        }

        return $"{first}.{last}";
    }

    private string GenerateUniqueLogin(string baseLogin)
    {
        if (string.IsNullOrWhiteSpace(baseLogin))
        {
            return string.Empty;
        }

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var allUsers = _authService.GetAllUsers();
        foreach (var user in allUsers)
        {
            if (!string.IsNullOrWhiteSpace(user.Login))
            {
                existing.Add(user.Login.Trim());
            }

            if (!string.IsNullOrWhiteSpace(user.Username))
            {
                existing.Add(user.Username.Trim());
            }
        }

        if (!existing.Contains(baseLogin))
        {
            return baseLogin;
        }

        var suffix = 2;
        while (true)
        {
            var candidate = $"{baseLogin}{suffix}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }

            suffix++;
        }
    }

    private bool IsCoreCompany(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
        {
            return false;
        }

        return string.Equals(
            companyName.Trim(),
            _appState.CoreCompanyName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Normalize(NormalizationForm.FormD);
        var filtered = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(filtered.ToArray()).Normalize(NormalizationForm.FormC);
    }

    private void UpdateCommandStates()
    {
        if (SaveUserCommand is RelayCommand saveCommand)
        {
            saveCommand.RaiseCanExecuteChanged();
        }

        if (DeleteUserCommand is RelayCommand deleteCommand)
        {
            deleteCommand.RaiseCanExecuteChanged();
        }
    }

    private bool CanExecuteSaveUser()
    {
        return !IsBusy;
    }

    private bool CanExecuteDeleteUser()
    {
        return SelectedUser != null;
    }

    public async Task<bool> SaveUserAsync()
    {
        IsBusy = true;
        UpdateCommandStates();

        var success = false;
        SetError(string.Empty);
        SetSuccess(string.Empty);

        try
        {
            if (!ValidateRequiredFields())
            {
                return false;
            }

            var user = BuildUserFromEditFields();
            UserAccount saved;

            if (IsNewUser || string.IsNullOrWhiteSpace(_currentEditUserId))
            {
                saved = await _authService.CreateUserAsync(user).ConfigureAwait(false);
            }
            else
            {
                var originalLogin = _currentEditUserLogin ?? user.Login ?? user.Username;
                saved = await _authService.UpdateUserAsync(originalLogin, user).ConfigureAwait(false);
            }

            _currentEditUserId = saved.Id;
            _currentEditUserLogin = saved.Login;
            IsNewUser = false;

            ReloadUsersFromService(saved.Username, saved.Id);
            SetSuccess("Uživatel byl uložen.");
            success = true;
            return true;
        }
        catch
        {
            SetError("Uložení uživatele se nezdařilo.");
            return false;
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
            UserSaved?.Invoke(this, success);
        }
    }

    private UserAccount BuildUserFromEditFields()
    {
        var rawUsername = EditUsername?.Trim() ?? string.Empty;
        var login = (EditLogin ?? string.Empty).Trim();
        var roleDomain = NormalizeRoleForDomain(EditRole);

        var username = string.IsNullOrWhiteSpace(rawUsername) ? login : rawUsername;

        return new UserAccount
        {
            Id = string.IsNullOrWhiteSpace(_currentEditUserId) ? Guid.NewGuid().ToString("D") : _currentEditUserId,
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(EditDisplayName) ? username : EditDisplayName.Trim(),
            Role = roleDomain,
            Login = login,
            WorkerNumber = EditWorkerNumber?.Trim() ?? string.Empty,
            TitleBefore = EditTitleBefore?.Trim() ?? string.Empty,
            FirstName = EditFirstName?.Trim() ?? string.Empty,
            LastName = EditLastName?.Trim() ?? string.Empty,
            TitleAfter = EditTitleAfter?.Trim() ?? string.Empty,
            JobTitle = EditJobTitle?.Trim() ?? string.Empty,
            CompanyName = EditCompanyName?.Trim() ?? string.Empty,
            CompanyAddress = EditCompanyAddress?.Trim() ?? string.Empty,
            PhoneNumber = EditPhoneNumber?.Trim() ?? string.Empty,
            Note = EditNote?.Trim() ?? string.Empty,
            IsActive = EditIsActive,
            EmploymentType = IsCoreCompany(EditCompanyName)
                ? "Kmenov� zam�stnanec"
                : "Subdodavatel"
        };
    }


    private bool ValidateRequiredFields()
    {
        var roleDomain = NormalizeRoleForDomain(EditRole);
        var missingCommon = string.IsNullOrWhiteSpace(EditLogin) ||
                            string.IsNullOrWhiteSpace(EditRole) ||
                            string.IsNullOrWhiteSpace(EditFirstName) ||
                            string.IsNullOrWhiteSpace(EditLastName);

        var missingWorkerSpecific = string.Equals(roleDomain, Roles.Delnik, StringComparison.OrdinalIgnoreCase) &&
                                    (string.IsNullOrWhiteSpace(EditWorkerNumber) ||
                                     string.IsNullOrWhiteSpace(EditCompanyName));

        if (missingCommon || missingWorkerSpecific)
        {
            SetError("Vyplňte prosím všechna povinná pole.");
            return false;
        }

        return true;
    }

    private void SetError(string message)
    {
        UserFormError = message;
        UserFormSuccess = string.Empty;
    }

    private void SetSuccess(string message)
    {
        UserFormSuccess = message;
        UserFormError = string.Empty;
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}



