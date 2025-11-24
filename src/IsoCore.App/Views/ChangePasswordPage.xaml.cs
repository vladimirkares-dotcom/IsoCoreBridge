using IsoCore.App.ViewModels;
using Microsoft.UI.Xaml;

namespace IsoCore.App.Views;

public sealed partial class ChangePasswordPage : MenuPage
{
    public ChangePasswordViewModel ViewModel { get; }

    public ChangePasswordPage()
    {
        InitializeComponent();

        ViewModel = new ChangePasswordViewModel(App.UserAuthService);
        DataContext = ViewModel;
    }

    private async void OnChangePasswordClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.OldPassword = OldPasswordBox.Password;
        ViewModel.NewPassword = NewPasswordBox.Password;
        ViewModel.ConfirmPassword = ConfirmPasswordBox.Password;

        await ViewModel.ChangePasswordAsync().ConfigureAwait(true);
    }
}
