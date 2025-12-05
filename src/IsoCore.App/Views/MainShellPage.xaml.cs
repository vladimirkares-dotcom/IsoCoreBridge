using IsoCore.App.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public sealed partial class MainShellPage : Page
{
    private PageRoute _currentRoute = PageRoute.Dashboard;

    public MainShellPage()
    {
        InitializeComponent();
        NavigateTo(PageRoute.Dashboard);
    }

    public Frame ContentFrameHost => ContentFrame;

    private void NavigateTo(PageRoute route)
    {
        var targetPageType = route switch
        {
            PageRoute.Dashboard => typeof(DashboardPage),
            PageRoute.Projects => typeof(ProjectsPage),
            PageRoute.SettingsUsers => typeof(UsersPage),
            PageRoute.SettingsBranding => typeof(BrandingPage),
            _ => null
        };

        if (targetPageType is null)
        {
            return;
        }

        if (ContentFrame.Content?.GetType() == targetPageType)
        {
            _currentRoute = route;
            return;
        }

        _currentRoute = route;
        ContentFrame.Navigate(targetPageType);
    }

    private void OnDashboardClicked(object sender, RoutedEventArgs e)
    {
        NavigateTo(PageRoute.Dashboard);
    }

    private void OnProjectsClicked(object sender, RoutedEventArgs e)
    {
        NavigateTo(PageRoute.Projects);
    }

    private void OnUsersClicked(object sender, RoutedEventArgs e)
    {
        NavigateTo(PageRoute.SettingsUsers);
    }

    private void OnBrandingClicked(object sender, RoutedEventArgs e)
    {
        NavigateTo(PageRoute.SettingsBranding);
    }
}
