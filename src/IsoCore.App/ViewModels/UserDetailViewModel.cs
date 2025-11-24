using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IsoCore.App.Services;
using IsoCore.App.State;
using IsoCore.Domain;
using Microsoft.UI.Xaml;

namespace IsoCore.App.ViewModels;

public class UserDetailViewModel : ViewModelBase
{
    private readonly IUserAuthService _authService;
    private readonly IAppStateService _appState;
    private UserAccount? _editingUser;
    private bool _isEditMode;
    private string _login = string.Empty;
    private string _workerNumber = string.Empty;
    private string _titleBefore = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _titleAfter = string.Empty;
    private string _jobTitle = string.Empty;
    private string _companyName = string.Empty;
    private string _companyAddress = string.Empty;
    private string _phoneNumber = string.Empty;
    private string _role = Roles.Technik;
    private bool _isActive = true;
    private string _validationMessage = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _passwordError = string.Empty;
    private string? _originalLogin;
    public bool IsNewUser => string.IsNullOrWhiteSpace(_originalLogin);
    public Visibility PasswordFieldsVisibility => IsNewUser ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ResetPasswordButtonVisibility => IsNewUser ? Visibility.Collapsed : Visibility.Visible;

    public UserDetailViewModel(IUserAuthService authService, IAppStateService appState)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        RoleOptions = Roles.All.Select(role => new RoleOption(role, Roles.GetDisplayName(role))).ToList();
        _appState.CurrentUserChanged += OnCurrentUserChanged;
    }

    public IReadOnlyList<RoleOption> RoleOptions { get; }

    public string HeaderText => IsEditMode ? "Editace uživatele" : "Nový uživatel";

    public bool IsEditMode
    {
        get => _isEditMode;
        private set
        {
            if (SetProperty(ref _isEditMode, value))
            {
                OnPropertyChanged(nameof(HeaderText));
            }
        }
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

    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    public string WorkerNumber
    {
        get => _workerNumber;
        set => SetProperty(ref _workerNumber, value);
    }

    public string TitleBefore
    {
        get => _titleBefore;
        set => SetProperty(ref _titleBefore, value);
    }

    public string FirstName
    {
        get => _firstName;
        set => SetProperty(ref _firstName, value);
    }

    public string LastName
    {
        get => _lastName;
        set => SetProperty(ref _lastName, value);
    }

    public string TitleAfter
    {
        get => _titleAfter;
        set => SetProperty(ref _titleAfter, value);
    }

    public string JobTitle
    {
        get => _jobTitle;
        set => SetProperty(ref _jobTitle, value);
    }

    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    public string CompanyAddress
    {
        get => _companyAddress;
        set => SetProperty(ref _companyAddress, value);
    }

    public string PhoneNumber
    {
        get => _phoneNumber;
        set => SetProperty(ref _phoneNumber, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => SetProperty(ref _confirmPassword, value);
    }

    public string PasswordError
    {
        get => _passwordError;
        private set => SetProperty(ref _passwordError, value);
    }

    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public void Initialize(string? username)
    {
        ValidationMessage = string.Empty;
        ClearPasswordInputs();

        if (!string.IsNullOrWhiteSpace(username))
        {
            var user = _authService.GetAllUsers()
                .FirstOrDefault(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

            if (user != null)
            {
                LoadFromUser(user);
                _editingUser = user;
                IsEditMode = true;
                return;
            }
        }

        ClearForm();
    }

    public async Task<bool> SaveAsync()
    {
        ValidationMessage = string.Empty;
        PasswordError = string.Empty;

        if (string.IsNullOrWhiteSpace(Login))
        {
            ValidationMessage = "Login / e-mail je povinný.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ValidationMessage = "Jméno i příjmení jsou povinné.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Role))
        {
            ValidationMessage = "Vyber roli.";
            return false;
        }

        var passwordProvided = IsNewUser && (!string.IsNullOrWhiteSpace(NewPassword) || !string.IsNullOrWhiteSpace(ConfirmPassword));

        if (IsNewUser && !passwordProvided)
        {
            PasswordError = "Zadej heslo pro nový účet.";
            return false;
        }

        if (passwordProvided && NewPassword != ConfirmPassword)
        {
            PasswordError = "Hesla se neshodují.";
            return false;
        }

        var displayName = $"{FirstName.Trim()} {LastName.Trim()}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = Login.Trim();
        }

        var user = new UserAccount
        {
            Id = _editingUser?.Id ?? string.Empty,
            Username = Login.Trim(),
            DisplayName = displayName,
            Login = Login.Trim(),
            WorkerNumber = WorkerNumber.Trim(),
            TitleBefore = TitleBefore.Trim(),
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            TitleAfter = TitleAfter.Trim(),
            JobTitle = JobTitle.Trim(),
            CompanyName = CompanyName.Trim(),
            CompanyAddress = CompanyAddress.Trim(),
            PhoneNumber = PhoneNumber.Trim(),
            Role = Role,
            IsActive = IsActive,
            Note = _editingUser?.Note ?? string.Empty,
            MustChangePassword = IsNewUser
                ? false
                : _editingUser?.MustChangePassword ?? false
        };

        UserAccount saved;
        if (IsNewUser)
        {
            saved = await _authService.CreateUserAsync(user);
        }
        else
        {
            saved = await _authService.UpdateUserAsync(_originalLogin!, user);
        }

        if (passwordProvided)
        {
            await _authService.SetPasswordAsync(saved.Username, NewPassword);
        }

        _editingUser = saved;
        IsEditMode = true;
        _originalLogin = saved.Username;
        NotifyUserModeChanged();

        if (_appState.CurrentUser != null && saved.Id == _appState.CurrentUser.Id)
        {
            _appState.SetCurrentUser(saved);
        }

        ValidationMessage = string.Empty;
        ClearPasswordInputs();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(string newPassword)
    {
        ValidationMessage = string.Empty;

        if (_editingUser == null)
        {
            ValidationMessage = "Nejdříve vyber uživatele.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            ValidationMessage = "Zadej nové heslo.";
            return false;
        }

        await _authService.ResetPasswordAsync(_editingUser.Username, newPassword);

        _editingUser.MustChangePassword = true;
        ValidationMessage = "Dočasné heslo bylo uloženo, uživatel si ho musí při příštím přihlášení změnit.";

        if (_appState.CurrentUser?.Id == _editingUser.Id)
        {
            _appState.SetCurrentUser(_editingUser);
        }

        return true;
    }

    public void ReportValidationMessage(string message)
    {
        ValidationMessage = message;
    }
    private void ClearForm()
    {
        _editingUser = null;
        Login = string.Empty;
        WorkerNumber = string.Empty;
        TitleBefore = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        TitleAfter = string.Empty;
        JobTitle = string.Empty;
        CompanyName = string.Empty;
        CompanyAddress = string.Empty;
        PhoneNumber = string.Empty;
        Role = Roles.Technik;
        IsActive = true;
        IsEditMode = false;
        _originalLogin = null;
        ClearPasswordInputs();
        NotifyUserModeChanged();
    }

    private void ClearPasswordInputs()
    {
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        PasswordError = string.Empty;
    }

    private void LoadFromUser(UserAccount user)
    {
        Login = string.IsNullOrWhiteSpace(user.Login) ? user.Username : user.Login;
        WorkerNumber = user.WorkerNumber;
        TitleBefore = user.TitleBefore;
        FirstName = user.FirstName;
        LastName = user.LastName;
        TitleAfter = user.TitleAfter;
        JobTitle = user.JobTitle;
        CompanyName = user.CompanyName;
        CompanyAddress = user.CompanyAddress;
        PhoneNumber = user.PhoneNumber;
        Role = user.Role;
        IsActive = user.IsActive;
        IsEditMode = true;
        _originalLogin = user.Username;
        NotifyUserModeChanged();
    }

    private void NotifyUserModeChanged()
    {
        OnPropertyChanged(nameof(IsNewUser));
        OnPropertyChanged(nameof(PasswordFieldsVisibility));
        OnPropertyChanged(nameof(ResetPasswordButtonVisibility));
    }

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentUserLabel));
    }
}
