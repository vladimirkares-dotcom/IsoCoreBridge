using System;
using System.Threading.Tasks;
using IsoCore.App.Services;

namespace IsoCore.App.ViewModels;

public class LoginViewModel : ViewModelBase
{
    public event EventHandler? LoginSucceeded;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public async Task<bool> LoginAsync()
    {
        if (IsBusy)
        {
            return false;
        }

        ErrorMessage = string.Empty;

        var username = (Username ?? string.Empty).Trim();
        var password = Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Zadejte \u017e\u00edvatelsk\u00e9 jm\u00e9no i heslo.";
            return false;
        }

        Username = username;

        IsBusy = true;
        try
        {
            var result = await App.UserAuthService.LoginAsync(username, password).ConfigureAwait(false);
            if (!result.Success || result.User == null)
            {
                ErrorMessage = result.ErrorMessage ?? "Nepoda\u0159ilo se p\u0159ihl\u00e1sit.";
                return false;
            }

            App.AppState.SetCurrentUser(result.User);
            LoginSucceeded?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception)
        {
            ErrorMessage = "Do\u0161lo k chyb\u011b p\u0159i p\u0159ihl\u00e1\u0161en\u00ed.";
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
