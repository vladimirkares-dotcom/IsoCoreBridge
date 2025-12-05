using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml;
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
    public string PersonalNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
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
    private string _editPhonePrefix = "+420";
    private string _editEmail = string.Empty;
    private string _editNote = string.Empty;
    private string _editPersonalNumber = string.Empty;
    private bool _editIsActive = true;
    private bool _isBusy;
    private bool _loginEditedManually;
    private bool _suppressLoginManualFlag;
    private bool _jobTitleEditedManually;
    private bool _suppressJobTitleManualFlag;
    private string _lastSuggestedJobTitle = string.Empty;
    private bool _isLoginReadOnly = true;
    private bool _emailEditedManually;
    private bool _suppressEmailManualFlag;
    private int _totalUsers;
    private int _activeUsers;
    private int _inactiveUsers;
    private int _coreEmployees;
    private int _subcontractors;
    private int _adminCount;
    private int _mistrCount;
    private int _kontrolorCount;
    private int _technikCount;
    private int _predakCount;
    private int _delnikCount;

    public Microsoft.UI.Xaml.Visibility AdminSectionVisibility => _appState.IsAdmin ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    private bool _isNewUser;
    private string _userFormError = string.Empty;
    private string _userFormSuccess = string.Empty;
    private readonly Dictionary<string, string> _displayToDomainRoleMap;
    private readonly Dictionary<string, string> _domainToDisplayRoleMap;
    private string? _currentEditUserId;
    private string? _currentEditUserLogin;
    private bool IsCreateMode => IsNewUser || string.IsNullOrWhiteSpace(_currentEditUserId);

    public ObservableCollection<UserListItem> Users { get; } = new();

    public IReadOnlyList<string> AvailableRoles { get; } =
        new[] { "Administrátor", "Mistr", "Kontrolor", "Technik", "Předák", "Dělník" };

    public IReadOnlyList<RoleOption> RoleOptions { get; }
    public IReadOnlyList<string> PhonePrefixes { get; }

    public ICommand NewUserCommand { get; }
    public ICommand SaveUserCommand { get; }
    public ICommand DeleteUserCommand { get; }
    public ICommand AllowLoginEditCommand { get; }

    public UserListItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                OnSelectedUserChanged(value);
                OnPropertyChanged(nameof(HasSelectedUser));
                OnPropertyChanged(nameof(HasNoSelectedUser));
                OnPropertyChanged(nameof(UserDetailVisibility));
                OnPropertyChanged(nameof(UserSummaryVisibility));
            }
        }
    }

    public bool HasSelectedUser => SelectedUser != null;
    public bool HasNoSelectedUser => SelectedUser == null;
    public Visibility UserDetailVisibility => HasSelectedUser ? Visibility.Visible : Visibility.Collapsed;
    public Visibility UserSummaryVisibility => HasSelectedUser ? Visibility.Collapsed : Visibility.Visible;
    public int TotalUsers
    {
        get => _totalUsers;
        private set => SetProperty(ref _totalUsers, value);
    }

    public int ActiveUsers
    {
        get => _activeUsers;
        private set => SetProperty(ref _activeUsers, value);
    }

    public int InactiveUsers
    {
        get => _inactiveUsers;
        private set => SetProperty(ref _inactiveUsers, value);
    }

    public int CoreEmployees
    {
        get => _coreEmployees;
        private set => SetProperty(ref _coreEmployees, value);
    }

    public int Subcontractors
    {
        get => _subcontractors;
        private set => SetProperty(ref _subcontractors, value);
    }

    public int AdminCount
    {
        get => _adminCount;
        private set => SetProperty(ref _adminCount, value);
    }

    public int MistrCount
    {
        get => _mistrCount;
        private set => SetProperty(ref _mistrCount, value);
    }

    public int KontrolorCount
    {
        get => _kontrolorCount;
        private set => SetProperty(ref _kontrolorCount, value);
    }

    public int TechnikCount
    {
        get => _technikCount;
        private set => SetProperty(ref _technikCount, value);
    }

    public int PredakCount
    {
        get => _predakCount;
        private set => SetProperty(ref _predakCount, value);
    }

    public int DelnikCount
    {
        get => _delnikCount;
        private set => SetProperty(ref _delnikCount, value);
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
                OnPropertyChanged(nameof(NonWorkerVisibility));
                OnPropertyChanged(nameof(WorkerOnlyVisibility));
                ApplyJobTitleSuggestionForRole();
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
                if (!_suppressLoginManualFlag)
                {
                    _loginEditedManually = true;
                }
                UpdateCommandStates();
                UpdateEmailFromLoginIfNeeded();
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
        set
        {
            if (SetProperty(ref _editJobTitle, value))
            {
                if (!_suppressJobTitleManualFlag)
                {
                    _jobTitleEditedManually = true;
                }
            }
        }
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

    public string EditPhonePrefix
    {
        get => _editPhonePrefix;
        set => SetProperty(ref _editPhonePrefix, value);
    }

    public string EditEmail
    {
        get => _editEmail;
        set
        {
            if (SetProperty(ref _editEmail, value))
            {
                if (!_suppressEmailManualFlag)
                {
                    _emailEditedManually = true;
                }
            }
        }
    }

    public string EditPersonalNumber
    {
        get => _editPersonalNumber;
        set => SetProperty(ref _editPersonalNumber, value);
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
        set
        {
            if (SetProperty(ref _userFormSuccess, value))
            {
                OnPropertyChanged(nameof(LastChangeMessage));
            }
        }
    }

    public string LastChangeMessage =>
        string.IsNullOrWhiteSpace(UserFormSuccess) ? "Zatím nejsou žádné změny." : UserFormSuccess;

    private bool IsWorkerRole =>
        string.Equals(NormalizeRoleForDomain(EditRole), Roles.Delnik, StringComparison.OrdinalIgnoreCase);

    public Visibility NonWorkerVisibility => IsWorkerRole ? Visibility.Collapsed : Visibility.Visible;
    public Visibility WorkerOnlyVisibility => IsWorkerRole ? Visibility.Visible : Visibility.Collapsed;
    public bool IsLoginReadOnly
    {
        get => _isLoginReadOnly;
        private set => SetProperty(ref _isLoginReadOnly, value);
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
        PhonePrefixes = new[] { _appState.DefaultPhonePrefix };
        _displayToDomainRoleMap = BuildDisplayToDomainRoleMap();
        _domainToDisplayRoleMap = BuildDomainToDisplayRoleMap();

        NewUserCommand = new RelayCommand(_ => StartCreateUser(), _ => !IsBusy);
        SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync().ConfigureAwait(false), _ => CanExecuteSaveUser());
        DeleteUserCommand = new RelayCommand(async _ => await DeleteSelectedUserAsync().ConfigureAwait(false), _ => CanExecuteDeleteUser());
        AllowLoginEditCommand = new RelayCommand(_ => EnableLoginEdit());

        ReloadUsersFromService();
        StartNewUser();
        _appState.CurrentUserChanged += OnCurrentUserChanged;
        _loginEditedManually = false;
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
                        Users.Add(ToUserListItem(u));
                    }
                    UpdateAggregates();
                });
            }
            else
            {
                Users.Clear();
                foreach (var u in allUsers)
                {
                    Users.Add(ToUserListItem(u));
                }
                UpdateAggregates();
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
                PersonalNumber = SelectedUser.PersonalNumber,
                Email = SelectedUser.Email,
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
                PhoneNumber = BuildFullPhoneNumber(EditPhonePrefix, EditPhoneNumber),
                PersonalNumber = EditPersonalNumber.Trim(),
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
            SetPhoneFieldsFromStored(SelectedUser.PhoneNumber);
            EditPersonalNumber = SelectedUser.PersonalNumber;
            EditEmail = SelectedUser.Email;
            EditNote = SelectedUser.Note;
            EditIsActive = SelectedUser.IsActive;
            IsNewUser = false;
            UserFormError = string.Empty;
            UserFormSuccess = string.Empty;
            _currentEditUserId = SelectedUser.Id;
            _currentEditUserLogin = string.IsNullOrWhiteSpace(SelectedUser.Login) ? SelectedUser.Username : SelectedUser.Login;
            _jobTitleEditedManually = !string.IsNullOrWhiteSpace(EditJobTitle);
            _lastSuggestedJobTitle = EditJobTitle;
            if (!_jobTitleEditedManually)
            {
                ApplyJobTitleSuggestionForRole();
            }
            IsLoginReadOnly = true;
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
        _loginEditedManually = false;
        _jobTitleEditedManually = false;
        _emailEditedManually = false;
        ApplyJobTitleSuggestionForRole();
        IsLoginReadOnly = true;
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
        _loginEditedManually = false;
        IsLoginReadOnly = true;
    }

    public async Task<bool> SaveCurrentUserAsync(string? newPassword, string? confirmPassword)
    {
        UserFormError = string.Empty;

        EnsureLoginForNewUser();

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
            PhoneNumber = BuildFullPhoneNumber(EditPhonePrefix, EditPhoneNumber),
            PersonalNumber = EditPersonalNumber.Trim(),
            Email = EditEmail.Trim(),
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
            var item = ToUserListItem(u);

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

        UpdateAggregates();

        SelectedUser = newlySelected;
        if (newlySelected == null)
        {
            StartNewUser();
        }
    }

    private UserListItem ToUserListItem(UserAccount user)
    {
        return new UserListItem
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = BuildDisplayName(user),
            Role = user.Role,
            Login = user.Login,
            WorkerNumber = user.WorkerNumber,
            TitleBefore = user.TitleBefore,
            FirstName = user.FirstName,
            LastName = user.LastName,
            TitleAfter = user.TitleAfter,
            JobTitle = user.JobTitle,
            CompanyName = user.CompanyName,
            CompanyAddress = user.CompanyAddress,
            PhoneNumber = user.PhoneNumber,
            PersonalNumber = user.PersonalNumber,
            Email = user.Email,
            Note = user.Note,
            IsActive = user.IsActive
        };
    }

    private static string BuildDisplayName(UserAccount user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName.Trim();
        }

        var first = user.FirstName?.Trim() ?? string.Empty;
        var last = user.LastName?.Trim() ?? string.Empty;
        var combined = $"{first} {last}".Trim();

        if (!string.IsNullOrWhiteSpace(combined))
        {
            return combined;
        }

        if (!string.IsNullOrWhiteSpace(user.Login))
        {
            return user.Login.Trim();
        }

        return (user.Username ?? string.Empty).Trim();
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
        _loginEditedManually = false;
        _jobTitleEditedManually = false;
        _emailEditedManually = false;
        _lastSuggestedJobTitle = string.Empty;
        IsLoginReadOnly = true;
        UpdateCommandStates();
        ApplyJobTitleSuggestionForRole();
    }

    private void ClearEditFields()
    {
        EditUsername = string.Empty;
        EditDisplayName = string.Empty;
        EditRole = AvailableRoles.First();
        SetEditLoginInternal(string.Empty);
        EditWorkerNumber = string.Empty;
        EditTitleBefore = string.Empty;
        EditFirstName = string.Empty;
        EditLastName = string.Empty;
        EditTitleAfter = string.Empty;
        EditJobTitle = string.Empty;
        EditCompanyName = _appState.CoreCompanyName;
        EditCompanyAddress = string.Empty;
        EditPhoneNumber = string.Empty;
        EditPhonePrefix = _appState.DefaultPhonePrefix;
        EditPersonalNumber = string.Empty;
        SetEditEmailInternal(string.Empty);
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
        var domainRoles = new[] { Roles.Administrator, Roles.Mistr, Roles.Kontrolor, Roles.Technik, Roles.Predak, Roles.Delnik };

        return AvailableRoles
            .Zip(domainRoles, (display, domain) => new { display, domain })
            .ToDictionary(p => p.display, p => p.domain, StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, string> BuildDomainToDisplayRoleMap()
    {
        var domainRoles = new[] { Roles.Administrator, Roles.Mistr, Roles.Kontrolor, Roles.Technik, Roles.Predak, Roles.Delnik };

        return domainRoles
            .Zip(AvailableRoles, (domain, display) => new { domain, display })
            .ToDictionary(p => p.domain, p => p.display, StringComparer.OrdinalIgnoreCase);
    }

    private void AutoGenerateLoginForNewUser()
    {
        if (!IsCreateMode)
        {
            return;
        }

        // Do not overwrite a login that the user already typed.
        if (_loginEditedManually)
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

        SetEditLoginInternal(uniqueLogin);
        AutoGenerateEmailForNewUser();
    }

    private void EnsureLoginForNewUser()
    {
        if (!IsCreateMode && !string.IsNullOrWhiteSpace(EditLogin))
        {
            return;
        }

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
        var uniqueLogin = GenerateUniqueLogin(baseLogin);

        if (!string.IsNullOrWhiteSpace(uniqueLogin))
        {
            SetEditLoginInternal(uniqueLogin);
            AutoGenerateEmailForNewUser();
        }
    }

    private void UpdateAggregates()
    {
        var total = Users.Count;
        var active = Users.Count(u => u.IsActive);
        var inactive = total - active;
        var core = Users.Count(u => IsCoreCompany(u.CompanyName));
        var sub = total - core;

        int admin = 0, mistr = 0, kontrolor = 0, technik = 0, predak = 0, delnik = 0;

        foreach (var u in Users)
        {
            switch (u.Role)
            {
                case var role when string.Equals(role, Roles.Administrator, StringComparison.OrdinalIgnoreCase):
                    admin++; break;
                case var role when string.Equals(role, Roles.Mistr, StringComparison.OrdinalIgnoreCase):
                    mistr++; break;
                case var role when string.Equals(role, Roles.Kontrolor, StringComparison.OrdinalIgnoreCase):
                    kontrolor++; break;
                case var role when string.Equals(role, Roles.Technik, StringComparison.OrdinalIgnoreCase):
                    technik++; break;
                case var role when string.Equals(role, Roles.Predak, StringComparison.OrdinalIgnoreCase):
                    predak++; break;
                case var role when string.Equals(role, Roles.Delnik, StringComparison.OrdinalIgnoreCase):
                    delnik++; break;
            }
        }

        TotalUsers = total;
        ActiveUsers = active;
        InactiveUsers = inactive;
        CoreEmployees = core;
        Subcontractors = sub;
        AdminCount = admin;
        MistrCount = mistr;
        KontrolorCount = kontrolor;
        TechnikCount = technik;
        PredakCount = predak;
        DelnikCount = delnik;
    }

    private void SetEditLoginInternal(string login)
    {
        _suppressLoginManualFlag = true;
        try
        {
            EditLogin = login;
        }
        finally
        {
            _suppressLoginManualFlag = false;
        }
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
            return $"{first}.";
        }

        if (string.IsNullOrWhiteSpace(first))
        {
            return $"{last}.";
        }

        return $"{first}.{last}";
    }

    private void AutoGenerateEmailForNewUser()
    {
        if (!IsCreateMode)
        {
            return;
        }

        if (_emailEditedManually)
        {
            return;
        }

        var domain = (_appState.EmailDomain ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(domain))
        {
            return;
        }

        var login = (EditLogin ?? string.Empty).Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(login))
        {
            var first = (EditFirstName ?? string.Empty).Trim();
            var last = (EditLastName ?? string.Empty).Trim();
            var baseLogin = BuildBaseLoginFromName(first, last).TrimEnd('.');
            login = baseLogin;
        }

        if (string.IsNullOrWhiteSpace(login))
        {
            return;
        }

        var email = $"{login}@{domain}";
        SetEditEmailInternal(email);
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

        foreach (var userItem in Users)
        {
            if (!string.IsNullOrWhiteSpace(userItem.Login))
            {
                existing.Add(userItem.Login.Trim());
            }

            if (!string.IsNullOrWhiteSpace(userItem.Username))
            {
                existing.Add(userItem.Username.Trim());
            }
        }

        if (!existing.Contains(baseLogin))
        {
            return baseLogin;
        }

        var suffix = 1;
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

    private void ApplyJobTitleSuggestionForRole()
    {
        var roleDomain = NormalizeRoleForDomain(EditRole);
        var suggestion = GetJobTitleSuggestion(roleDomain);

        if (_jobTitleEditedManually &&
            !string.IsNullOrWhiteSpace(EditJobTitle) &&
            !string.Equals(EditJobTitle, _lastSuggestedJobTitle, StringComparison.OrdinalIgnoreCase))
        {
            _lastSuggestedJobTitle = suggestion;
            return;
        }

        _lastSuggestedJobTitle = suggestion;
        _suppressJobTitleManualFlag = true;
        try
        {
            EditJobTitle = suggestion;
        }
        finally
        {
            _suppressJobTitleManualFlag = false;
        }
        _jobTitleEditedManually = false;
    }

    private static string GetJobTitleSuggestion(string roleDomain)
    {
        if (string.Equals(roleDomain, Roles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            return "Stavbyvedoucí";
        }

        if (string.Equals(roleDomain, Roles.Mistr, StringComparison.OrdinalIgnoreCase))
        {
            return "Mistr stavební výroby";
        }

        if (string.Equals(roleDomain, Roles.Technik, StringComparison.OrdinalIgnoreCase))
        {
            return "Mistr stavební výroby";
        }

        if (string.Equals(roleDomain, Roles.Predak, StringComparison.OrdinalIgnoreCase))
        {
            return "Předák";
        }

        if (string.Equals(roleDomain, Roles.Kontrolor, StringComparison.OrdinalIgnoreCase))
        {
            return "Kontrolor";
        }

        return string.Empty;
    }

    private static string BuildFullPhoneNumber(string? prefix, string? number)
    {
        var cleanPrefix = prefix?.Trim() ?? string.Empty;
        var cleanNumber = number?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cleanPrefix) && string.IsNullOrWhiteSpace(cleanNumber))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(cleanPrefix))
        {
            return cleanNumber;
        }

        if (string.IsNullOrWhiteSpace(cleanNumber))
        {
            return cleanPrefix;
        }

        return $"{cleanPrefix} {cleanNumber}";
    }

    private void SetPhoneFieldsFromStored(string? stored)
    {
        var value = stored?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            EditPhonePrefix = PhonePrefixes.First();
            EditPhoneNumber = string.Empty;
            return;
        }

        var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && parts[0].StartsWith("+", StringComparison.Ordinal))
        {
            EditPhonePrefix = parts[0];
            EditPhoneNumber = parts.Length > 1 ? parts[1] : string.Empty;
        }
        else
        {
            EditPhonePrefix = PhonePrefixes.First();
            EditPhoneNumber = value;
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
        var isCreateOperation = IsCreateMode;
        SetError(string.Empty);
        SetSuccess(string.Empty);

        try
        {
            EnsureLoginForNewUser();
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
            var successMessage = isCreateOperation ? "Založen uživatel." : "Uživatel byl uložen.";
            SetSuccess(successMessage);
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
            PhoneNumber = BuildFullPhoneNumber(EditPhonePrefix, EditPhoneNumber),
            PersonalNumber = EditPersonalNumber?.Trim() ?? string.Empty,
            Email = EditEmail?.Trim() ?? string.Empty,
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

    private void EnableLoginEdit()
    {
        IsLoginReadOnly = false;
    }

    private void UpdateEmailFromLoginIfNeeded()
    {
        if (_emailEditedManually)
        {
            return;
        }

        var domain = (_appState.EmailDomain ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(domain))
        {
            return;
        }

        var login = (EditLogin ?? string.Empty).Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(login))
        {
            return;
        }

        SetEditEmailInternal($"{login}@{domain}");
    }

    private void SetEditEmailInternal(string value)
    {
        _suppressEmailManualFlag = true;
        try
        {
            EditEmail = value;
        }
        finally
        {
            _suppressEmailManualFlag = false;
        }
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



