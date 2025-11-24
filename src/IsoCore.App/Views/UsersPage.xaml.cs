using IsoCore.App;
using IsoCore.App.Services;
using IsoCore.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public sealed partial class UsersPage : MenuPage
{
    private readonly IAppStateService _appState;
    public UsersViewModel ViewModel { get; }

    public UsersPage()
    {
        InitializeComponent();

        _appState = App.AppState;
        ViewModel = new UsersViewModel(App.UserAuthService, _appState);
        DataContext = ViewModel;

        Loaded += UsersPage_Loaded;
    }

    private async void UsersPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= UsersPage_Loaded;

        try
        {
            await ViewModel.LoadUsersAsync();
        }
        catch
        {
            // Intentionally ignore errors here to avoid crashing the app.
        }
    }

    private void NewUserButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.StartNewUser();
        NewPasswordBox.Password = string.Empty;
        ConfirmPasswordBox.Password = string.Empty;

        // clear previous error message
        SetUserFormError(string.Empty);
    }

    private async void SaveUserButton_Click(object sender, RoutedEventArgs e)
    {
        var newPassword = NewPasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;

        SetUserFormError(string.Empty);

        var success = await ViewModel.SaveCurrentUserAsync(newPassword, confirmPassword).ConfigureAwait(true);
        if (success)
        {
            NewPasswordBox.Password = string.Empty;
            ConfirmPasswordBox.Password = string.Empty;
            SetUserFormError(string.Empty);
        }
        // If not successful, ViewModel.UserFormError already contains the error text.
    }

    private async void DeleteUserButton_Click(object sender, RoutedEventArgs e)
    {
        SetUserFormError(string.Empty);

        if (ViewModel.SelectedUser == null)
        {
            SetUserFormError("Není vybraný žádný uživatel pro smazání.");
            return;
        }

        await ViewModel.DeleteSelectedUserAsync().ConfigureAwait(true);
    }

    private async void ToggleActiveButton_Click(object sender, RoutedEventArgs e)
    {
        SetUserFormError(string.Empty);

        if (ViewModel.SelectedUser == null)
        {
            SetUserFormError("Není vybraný žádný uživatel.");
            return;
        }

        await ViewModel.ToggleActiveForSelectedAsync().ConfigureAwait(true);
    }

    private void SetUserFormError(string message)
    {
        if (ViewModel != null)
        {
            ViewModel.UserFormError = message;
        }
    }
}
