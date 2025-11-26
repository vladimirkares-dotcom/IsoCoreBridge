using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public sealed partial class MainShellPage : Page
{
    public MainShellPage()
    {
        InitializeComponent();
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    public Frame ContentFrameHost => ContentFrame;

    private void OnDashboardClicked(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.Content?.GetType() != typeof(DashboardPage))
        {
            ContentFrame.Navigate(typeof(DashboardPage));
        }
    }

    private void OnProjectsClicked(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.Content?.GetType() != typeof(ProjectsPage))
        {
            ContentFrame.Navigate(typeof(ProjectsPage));
        }
    }
}
