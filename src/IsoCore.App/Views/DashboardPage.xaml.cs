using System;
using System.Collections.Specialized;
using System.Linq;
using IsoCore.App.Services;
using IsoCore.App.State;
using IsoCore.App.ViewModels;
using IsoCore.Domain;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace IsoCore.App.Views;

public sealed partial class DashboardPage : MenuPage
{
    private TextBlock? _currentProjectTitleText;
    private TextBlock? _currentProjectBodyText;
    private StackPanel? _projectsListPanel;
    private readonly IAppStateService _appState;
    public DashboardViewModel ViewModel { get; }
    private bool _projectsInitialized;
    private bool _isLoadingProjects;

    public DashboardPage()
    {
        InitializeComponent();

        _appState = App.AppState;
        ViewModel = new DashboardViewModel(_appState);
        DataContext = ViewModel;
        UpdateCurrentDate();
        _appState.CurrentDateChanged += OnAppStateDateChanged;

        try
        {
            BuildCards();
            UpdateCurrentProjectCard();
            UpdateProjectsList();

            _appState.CurrentProjectChanged += OnProjectChanged;
            _appState.CurrentDateChanged += OnDateChanged;
            _appState.CurrentBuildingObjectChanged += OnBuildingObjectChanged;
            var projects = _appState.ProjectRegistry?.Projects;
            if (projects != null)
            {
                projects.CollectionChanged += OnProjectsCollectionChanged;
            }
        }
        catch (Exception ex)
        {
            ShowDashboardError(ex);
        }
    }

    private void BuildCards()
    {
        ClearCardSlots();

        if (LeftTopCardHost != null)
        {
            LeftTopCardHost.Child = CreateProgressCardContent();
        }

        if (LeftBottomCardHost != null)
        {
            LeftBottomCardHost.Child = CreateCurrentProjectCardContent();
        }

        if (RightBigCardHost != null)
        {
            RightBigCardHost.Child = CreateProjectsOverviewCardContent();
        }

        if (DashboardErrorArea != null)
        {
            DashboardErrorArea.Children.Clear();
            DashboardErrorArea.Visibility = Visibility.Collapsed;
        }
    }

    private UIElement CreateCurrentProjectCardContent()
    {
        _currentProjectTitleText = CreatePanelHeader("Aktuální projekt");

        _currentProjectBodyText = new TextBlock
        {
            Text = "Zatím není vybraný žádný projekt.",
            TextWrapping = TextWrapping.Wrap,
            Style = (Style)Application.Current.Resources["IcbdBodyMutedTextBlockStyle"]
        };

        var panel = new StackPanel
        {
            Spacing = 6
        };
        panel.Children.Add(_currentProjectTitleText);
        panel.Children.Add(_currentProjectBodyText);

        return panel;
    }

    private UIElement CreateProjectsOverviewCardContent()
    {
        var title = CreatePanelHeader("Projekty ve vývoji");

        _projectsListPanel = new StackPanel
        {
            Spacing = 4
        };

        _projectsListPanel.Children.Add(new TextBlock
        {
            Text = "Načítám projekty...",
            Style = (Style)Application.Current.Resources["IcbdBodyMutedTextBlockStyle"]
        });

        var container = new StackPanel
        {
            Spacing = 6
        };
        container.Children.Add(title);
        container.Children.Add(_projectsListPanel);

        return container;
    }

    private UIElement CreateProgressCardContent()
    {
        var projects = _appState.ProjectRegistry?.Projects;
        var total = projects?.Count ?? 0;
        var preparationCount = projects?.Count(p => p.Status == ProjectStatus.Preparation) ?? 0;
        var executionCount = projects?.Count(p => p.Status == ProjectStatus.Execution) ?? 0;
        var completedCount = projects?.Count(p => p.Status == ProjectStatus.Completed) ?? 0;

        var container = new StackPanel
        {
            Spacing = 8
        };

        container.Children.Add(CreatePanelHeader("Stav progresu"));
        container.Children.Add(new TextBlock
        {
            Text = $"Projekty celkem: {total}",
            Opacity = 0.75,
            Style = (Style)Application.Current.Resources["IcbdBodyTextBlockStyle"]
        });

        container.Children.Add(CreateProgressStatRow("V přípravě", preparationCount, total));
        container.Children.Add(CreateProgressStatRow("V realizaci", executionCount, total));
        container.Children.Add(CreateProgressStatRow("Dokončeno", completedCount, total));

        return container;
    }

    private UIElement CreateProgressStatRow(string label, int count, int total)
    {
        var statContainer = new StackPanel
        {
            Spacing = 2
        };

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var labelText = new TextBlock
        {
            Text = label,
            Opacity = 0.75,
            FontSize = 12,
            Style = (Style)Application.Current.Resources["IcbdBodyTextBlockStyle"]
        };
        Grid.SetColumn(labelText, 0);

        var countText = new TextBlock
        {
            Text = count.ToString(),
            FontWeight = FontWeights.SemiBold,
            Opacity = 0.85,
            FontSize = 12,
            Style = (Style)Application.Current.Resources["IcbdBodyTextBlockStyle"]
        };
        Grid.SetColumn(countText, 1);

        header.Children.Add(labelText);
        header.Children.Add(countText);

        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = Math.Max(total, 1),
            Value = count,
            Height = 4,
            Margin = new Thickness(0, 2, 0, 0)
        };

        statContainer.Children.Add(header);
        statContainer.Children.Add(progressBar);

        return statContainer;
    }

    private void ClearCardSlots()
    {
        if (LeftTopCardHost != null)
        {
            LeftTopCardHost.Child = null;
        }

        if (LeftBottomCardHost != null)
        {
            LeftBottomCardHost.Child = null;
        }

        if (RightBigCardHost != null)
        {
            RightBigCardHost.Child = null;
        }
    }

    private void ShowDashboardError(Exception ex)
    {
        ClearCardSlots();

        if (DashboardErrorArea == null)
        {
            return;
        }

        DashboardErrorArea.Children.Clear();
        DashboardErrorArea.Children.Add(new TextBlock
        {
            Text = $"Chyba při načítání dashboardu: {ex.Message}",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.8
        });
        DashboardErrorArea.Visibility = Visibility.Visible;
    }

    private TextBlock CreatePanelHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            Style = (Style)Application.Current.Resources["IcbdTitleMTextBlockStyle"]
        };
    }

    private void UpdateCurrentProjectCard()
    {
        if (_currentProjectTitleText == null || _currentProjectBodyText == null)
        {
            return;
        }

        var project = _appState.CurrentProject;
        var date = _appState.CurrentDate;
        var so = _appState.CurrentBuildingObject;

        if (project == null)
        {
            _currentProjectTitleText.Text = "Aktuální projekt";
            _currentProjectBodyText.Text =
                "Zatím není vybraný žádný projekt.\n" +
                "Otevři stránku Projekty a zvol projekt, se kterým chceš pracovat.";
            return;
        }

        var projectName = project.DisplayName ?? project.ProjectName;
        var objects = project.BuildingObjects;
        var soCount = objects?.Count ?? 0;

        var baseTitle = $"Aktuální projekt: {projectName} ({date:dd.MM.yyyy})";
        _currentProjectTitleText.Text = baseTitle;

        if (so == null)
        {
            _currentProjectBodyText.Text =
                $"Počet objektů (SO): {soCount}.\n" +
                "Vyber objekt v Projektech nebo na stránce Přehledy.";
            return;
        }

        var soName = so.DisplayName ?? so.Name;
        _currentProjectBodyText.Text =
            $"Vybraný objekt: {soName}\n" +
            $"Celkem objektů (SO): {soCount}.";
    }

    private void OnProjectChanged(object? sender, ProjectInfo? project)
    {
        UpdateCurrentProjectCard();
    }

    private void OnDateChanged(object? sender, DateOnly e)
    {
        UpdateCurrentProjectCard();
        UpdateCurrentDate();
    }

    private void OnAppStateDateChanged(object? sender, DateOnly e)
    {
        UpdateCurrentDate();
    }

    private void OnBuildingObjectChanged(object? sender, BuildingObjectInfo? obj)
    {
        UpdateCurrentProjectCard();
    }

    private void OnProjectShortcutClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ProjectInfo project)
        {
            return;
        }

        _appState.SetCurrentProject(project);
        OnProjectsClicked(this, new RoutedEventArgs());
    }

    protected override void OnProjectsClicked(object sender, RoutedEventArgs e)
    {
        NavigateTo<ProjectsPage>();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        _appState.Logout();
        // TODO: navigate to LoginPage when the page is implemented.
        // Frame?.Navigate(typeof(LoginPage));
    }

    private void OnProjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateProjectsList();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateCurrentDate();
    }

    private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_projectsInitialized || _isLoadingProjects)
        {
            return;
        }

        var registry = _appState.ProjectRegistry;
        if (registry == null)
        {
            return;
        }

        var projects = registry.Projects;
        if (projects != null && projects.Count > 0)
        {
            _projectsInitialized = true;
            return;
        }

        _isLoadingProjects = true;
        try
        {
            await registry.LoadFromStorageAsync().ConfigureAwait(true);
            _projectsInitialized = true;
        }
        catch (Exception ex)
        {
            ShowDashboardError(ex);
        }
        finally
        {
            _isLoadingProjects = false;
        }
    }

    private void UpdateCurrentDate()
    {
        if (CurrentDateText == null)
        {
            return;
        }

        var date = _appState.CurrentDate;
        CurrentDateText.Text = $"Dnešní datum: {date:dd.MM.yyyy}";
    }

    private void UpdateProjectsList()
    {
        if (_projectsListPanel == null)
        {
            return;
        }

        _projectsListPanel.Children.Clear();

        var projects = _appState.ProjectRegistry?.Projects;
        if (projects == null || projects.Count == 0)
        {
            _projectsListPanel.Children.Add(new TextBlock
            {
                Text = "Zatím nejsou žádné projekty.",
                Style = (Style)Application.Current.Resources["IcbdBodyMutedTextBlockStyle"]
            });
            return;
        }

        foreach (var project in projects)
        {
            var displayName = project.DisplayName ?? project.ProjectName ?? "(bez názvu)";

            var btn = new Button
            {
                Content = displayName,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 2),
                Tag = project
            };

            btn.Click += OnProjectShortcutClicked;

            _projectsListPanel.Children.Add(btn);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _appState.CurrentProjectChanged -= OnProjectChanged;
        _appState.CurrentDateChanged -= OnDateChanged;
        _appState.CurrentDateChanged -= OnAppStateDateChanged;
        _appState.CurrentBuildingObjectChanged -= OnBuildingObjectChanged;
        var projects = _appState.ProjectRegistry?.Projects;
        if (projects != null)
        {
            projects.CollectionChanged -= OnProjectsCollectionChanged;
        }
    }
}
