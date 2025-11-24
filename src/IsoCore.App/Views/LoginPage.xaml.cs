using System;
using System.Threading.Tasks;
using IsoCore.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public sealed partial class LoginPage : Page
{
    private readonly LoginViewModel _vm;
    private Button? _loginButton;

    public LoginPage()
    {
        _vm = new LoginViewModel();
        _vm.LoginSucceeded += OnLoginSucceeded;
        DataContext = _vm;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (FindName("LoginButton") is Button btn)
        {
            _loginButton = btn;
            btn.Click += OnLoginButtonClick;
        }
    }

    private void OnLoginSucceeded(object? sender, EventArgs e)
    {
        var frame = App.MainWindow.Content as Frame;
        frame?.Navigate(typeof(MainPage));
    }

    private async void OnLoginButtonClick(object sender, RoutedEventArgs e)
    {
        await OnLoginClicked();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_loginButton is not null)
        {
            _loginButton.Click -= OnLoginButtonClick;
        }

        _vm.LoginSucceeded -= OnLoginSucceeded;
    }

    public async Task OnLoginClicked()
    {
        if (_vm.IsBusy)
        {
            return;
        }

        if (_loginButton is not null)
        {
            _loginButton.IsEnabled = false;
        }

        try
        {
            await _vm.LoginAsync();
        }
        finally
        {
            if (_loginButton is not null)
            {
                _loginButton.IsEnabled = true;
            }
        }
    }
}
