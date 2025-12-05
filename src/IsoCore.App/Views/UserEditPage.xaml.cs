using System;
using IsoCore.App.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IsoCore.App.Views;

public sealed partial class UserEditPage : MenuPage
{
    public UsersViewModel? ViewModel { get; private set; }

    public UserEditPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is UsersViewModel vm)
        {
            AttachViewModel(vm);
        }
        else
        {
            // If no UsersViewModel is provided, keep the existing instance (if any).
            // This allows the page to survive back/forward navigation without breaking.
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        DetachViewModel();
    }

    private void AttachViewModel(UsersViewModel vm)
    {
        if (vm is null)
        {
            throw new ArgumentNullException(nameof(vm));
        }

        // If we already have this VM attached, do nothing.
        if (ReferenceEquals(ViewModel, vm))
        {
            return;
        }

        // Unsubscribe from the previous VM, if any.
        if (ViewModel is not null)
        {
            ViewModel.UserSaved -= OnUserSaved;
        }

        ViewModel = vm;
        ViewModel.UserSaved += OnUserSaved;
        DataContext = ViewModel;
    }

    private void DetachViewModel()
    {
        if (ViewModel is not null)
        {
            ViewModel.UserSaved -= OnUserSaved;
        }
    }

    private void OnUserSaved(object? sender, bool success)
    {
        var dispatcher = DispatcherQueue;
        if (dispatcher != null)
        {
            dispatcher.TryEnqueue(() =>
            {
                if (success)
                {
                    Frame?.GoBack();
                }
            });
        }
        else if (success)
        {
            Frame?.GoBack();
        }
    }
}
