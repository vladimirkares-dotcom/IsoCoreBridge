using IsoCore.App.Services;
using IsoCore.App.ViewModels;

namespace IsoCore.App.Views;

public sealed partial class BrandingPage : MenuPage
{
    public CompanySettingsViewModel ViewModel { get; }

    public BrandingPage()
    {
        InitializeComponent();
        ViewModel = new CompanySettingsViewModel(App.AppState);
        DataContext = ViewModel;
    }
}
