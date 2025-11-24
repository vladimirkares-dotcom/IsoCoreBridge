using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IsoCore.App.Views;

public abstract partial class MenuPage : Page
{
    // Public parameterless constructor – required for XAML / x:Bind
    protected MenuPage()
    {
        // No initialization needed; existence of a public ctor is enough.
    }

    protected void NavigateTo(Type pageType)
    {
        Frame?.Navigate(pageType);
    }

    protected void NavigateTo<TPage>() where TPage : Page
    {
        NavigateTo(typeof(TPage));
    }

    // Virtual handlers for left menu clicks – can be overridden in derived pages
    protected virtual void OnDashboardClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnProjectsClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnOverviewsClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnRecordsClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnDiaryClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnExportsClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnUsersClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnTemplatesClicked(object sender, RoutedEventArgs e) { }
    protected virtual void OnSettingsClicked(object sender, RoutedEventArgs e) { }
}
