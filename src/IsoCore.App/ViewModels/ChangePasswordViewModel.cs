using System;
using System.Threading.Tasks;
using IsoCore.App.Services;

namespace IsoCore.App.ViewModels;

public class ChangePasswordViewModel : ViewModelBase
{
    private readonly IUserAuthService _authService;
    private string _oldPassword = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isBusy;

    public ChangePasswordViewModel(IUserAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    public string OldPassword
    {
        get => _oldPassword;
        set => SetProperty(ref _oldPassword, value);
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

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        private set => SetProperty(ref _successMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        SuccessMessage = string.Empty;
    }

    private void SetSuccess(string message)
    {
        SuccessMessage = message;
        ErrorMessage = string.Empty;
    }

    public async Task<bool> ChangePasswordAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        SetError(string.Empty);
        SetSuccess(string.Empty);

        var user = App.AppState.CurrentUser;
        if (user == null)
        {
            SetError("Žádný přihlášený uživatel.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(OldPassword))
        {
            SetError("Zadejte staré heslo.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            SetError("Zadejte nové heslo.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            SetError("Potvrďte nové heslo.");
            return false;
        }

        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            SetError("Hesla se neshodují.");
            return false;
        }

        if (NewPassword.Length < 6)
        {
            SetError("Heslo musí mít alespoň 6 znaků.");
            return false;
        }

        IsBusy = true;
        try
        {
            // Verify old password by attempting login.
            var loginResult = await _authService.LoginAsync(user.Username, OldPassword).ConfigureAwait(false);
            if (!loginResult.Success)
            {
                SetError(loginResult.ErrorMessage ?? "Změna hesla se nezdařila.");
                return false;
            }

            await _authService.SetPasswordAsync(user.Username, NewPassword).ConfigureAwait(false);

            SetSuccess("Heslo bylo úspěšně změněno.");
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            return true;
        }
        catch (Exception)
        {
            SetError("Změna hesla se nezdařila.");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
