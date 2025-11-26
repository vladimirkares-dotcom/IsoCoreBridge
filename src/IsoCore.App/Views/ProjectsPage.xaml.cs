using IsoCore.App.Services;
using IsoCore.App.ViewModels;
using IsoCore.Domain;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace IsoCore.App.Views;

public sealed partial class ProjectsPage : MenuPage
{
    private readonly IAppStateService _appState;
    public ProjectsViewModel ViewModel { get; }

    public ProjectsPage()
    {
        InitializeComponent();
        _appState = App.AppState;
        ViewModel = new ProjectsViewModel(_appState);
        DataContext = ViewModel;
        Loaded += ProjectsPage_Loaded;
        UpdateActionButtons(null);
    }

    private async void ProjectsPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ProjectsPage_Loaded;
        await ViewModel.LoadProjectsAsync();
    }

    private async void DeleteProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedProject == null)
        {
            return;
        }

        var project = ViewModel.SelectedProject;
        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Smazat projekt",
            Content = $"Opravdu chcete smazat projekt \"{project.DisplayName}\"?",
            PrimaryButtonText = "Smazat",
            CloseButtonText = "Zrušit",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.DeleteSelectedProjectAsync();
    }

    private async void EditProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedProject == null)
        {
            return;
        }

        var selected = ViewModel.SelectedProject;

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Editovat projekt",
            PrimaryButtonText = "Uložit",
            CloseButtonText = "Zrušit",
            DefaultButton = ContentDialogButton.Primary
        };

        var panel = new StackPanel
        {
            Spacing = 8
        };

        var codeBox = new TextBox
        {
            Header = "Kód projektu",
            Text = selected.Code,
            PlaceholderText = "Např. D35-2025-01"
        };

        var nameBox = new TextBox
        {
            Header = "Název projektu",
            Text = selected.Name,
            PlaceholderText = "Např. D35 – Most SO 202"
        };

        var errorText = new TextBlock();

        void Validate()
        {
            var code = codeBox.Text?.Trim();
            var name = nameBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                errorText.Text = "Vyplňte kód i název projektu.";
                dialog.IsPrimaryButtonEnabled = false;
                return;
            }

            var duplicate = ViewModel.Projects.Any(p =>
                !ReferenceEquals(p, selected) &&
                string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase));

            if (duplicate)
            {
                errorText.Text = "Projekt s tímto kódem již existuje.";
                dialog.IsPrimaryButtonEnabled = false;
                return;
            }

            errorText.Text = string.Empty;
            dialog.IsPrimaryButtonEnabled = true;
        }

        codeBox.TextChanged += (_, __) => Validate();
        nameBox.TextChanged += (_, __) => Validate();

        panel.Children.Add(codeBox);
        panel.Children.Add(nameBox);
        panel.Children.Add(errorText);

        dialog.Content = panel;

        dialog.IsPrimaryButtonEnabled = true;
        Validate();

        dialog.PrimaryButtonClick += async (_, args) =>
        {
            Validate();
            if (!dialog.IsPrimaryButtonEnabled)
            {
                args.Cancel = true;
                return;
            }

            var code = codeBox.Text?.Trim() ?? string.Empty;
            var name = nameBox.Text?.Trim() ?? string.Empty;

            var updated = await ViewModel.UpdateSelectedProjectAsync(code, name);
            if (!updated)
            {
                errorText.Text = "Nelze uložit změny projektu.";
                dialog.IsPrimaryButtonEnabled = false;
                args.Cancel = true;
            }
        };

        await dialog.ShowAsync();
    }

    private async void OnProjectItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        if (sender is FrameworkElement element && element.DataContext is ProjectInfo project)
        {
            ViewModel.SelectedProject = project;
            ViewModel.OpenProject(project);
            NavigateTo<ProjectDetailPage>();
        }

        await Task.CompletedTask;
    }

    private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = Content.XamlRoot,
            Title = "Založit projekt",
            PrimaryButtonText = "Vytvořit",
            CloseButtonText = "Zrušit",
            DefaultButton = ContentDialogButton.Primary
        };

        var panel = new StackPanel
        {
            Spacing = 8
        };

        var codeBox = new TextBox
        {
            Header = "Kód projektu",
            PlaceholderText = "Např. D35-2025-01"
        };

        var nameBox = new TextBox
        {
            Header = "Název projektu",
            PlaceholderText = "Např. D35 – Most SO 202"
        };

        panel.Children.Add(codeBox);
        panel.Children.Add(nameBox);

        dialog.Content = panel;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            return;
        }

        var code = codeBox.Text?.Trim();
        var name = nameBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            code = Guid.NewGuid().ToString("N");
        }

        await ViewModel.CreateAndAddProjectAsync(code, name);
    }

    private void OnProjectSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ProjectsViewModel vm)
        {
            return;
        }

        var selected = (sender as ListView)?.SelectedItem as ProjectInfo;
        if (selected != null)
        {
            vm.SetCurrentProject(selected);
        }

        UpdateActionButtons(selected);
    }

    private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProjectsViewModel vm)
        {
            return;
        }

        var selected = vm.SelectedProject;
        if (selected == null)
        {
            return;
        }

        vm.OpenProject(selected);

        NavigateTo<ProjectDetailPage>();
    }

    private void UpdateActionButtons(ProjectInfo? selected)
    {
        var hasSelection = selected != null;
        EditProjectButton.IsEnabled = hasSelection;
        DeleteProjectButton.IsEnabled = hasSelection;
        OpenProjectButton.IsEnabled = hasSelection;
    }
}
