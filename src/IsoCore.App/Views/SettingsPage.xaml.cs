using IsoCore.App.ViewModels;
using Microsoft.UI.Xaml;

namespace IsoCore.App.Views;

public sealed partial class SettingsPage : MenuPage
{
    public SettingsPageViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = new SettingsPageViewModel(App.UserAuthService);
        DataContext = ViewModel;

        Loaded += OnSettingsPageLoaded;
    }

    private void OnSettingsPageLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.RefreshFromAppState();
    }
}
