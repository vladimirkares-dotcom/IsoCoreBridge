using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Controls;

public sealed partial class LeftNavigationMenu : UserControl
{
    public static readonly DependencyProperty AdminSectionVisibilityProperty =
        DependencyProperty.Register(
            nameof(AdminSectionVisibility),
            typeof(Visibility),
            typeof(LeftNavigationMenu),
            new PropertyMetadata(Visibility.Collapsed));

    public Visibility AdminSectionVisibility
    {
        get => (Visibility)GetValue(AdminSectionVisibilityProperty);
        set => SetValue(AdminSectionVisibilityProperty, value);
    }

    public LeftNavigationMenu()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler? DashboardClicked;
    public event RoutedEventHandler? ProjectsClicked;
    public event RoutedEventHandler? OverviewsClicked;
    public event RoutedEventHandler? RecordsClicked;
    public event RoutedEventHandler? DiaryClicked;
    public event RoutedEventHandler? ExportsClicked;
    public event RoutedEventHandler? UsersClicked;
    public event RoutedEventHandler? TemplatesClicked;
    public event RoutedEventHandler? SettingsClicked;

    private void OnDashboardClicked(object sender, RoutedEventArgs e)
    {
        DashboardClicked?.Invoke(this, e);
    }

    private void OnProjectsClicked(object sender, RoutedEventArgs e)
    {
        ProjectsClicked?.Invoke(this, e);

        // navigation to ProjectsPage temporarily disabled
    }

    private void OnOverviewsClicked(object sender, RoutedEventArgs e)
    {
        OverviewsClicked?.Invoke(this, e);
    }

    private void OnRecordsClicked(object sender, RoutedEventArgs e)
    {
        RecordsClicked?.Invoke(this, e);
    }

    private void OnDiaryClicked(object sender, RoutedEventArgs e)
    {
        DiaryClicked?.Invoke(this, e);
    }

    private void OnExportsClicked(object sender, RoutedEventArgs e)
    {
        ExportsClicked?.Invoke(this, e);
    }

    private void OnUsersClicked(object sender, RoutedEventArgs e)
    {
        UsersClicked?.Invoke(this, e);
    }

    private void OnTemplatesClicked(object sender, RoutedEventArgs e)
    {
        TemplatesClicked?.Invoke(this, e);
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        SettingsClicked?.Invoke(this, e);
    }
}
