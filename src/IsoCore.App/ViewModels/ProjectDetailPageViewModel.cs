using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Input;
using IsoCore.App.Services;
using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public class ProjectDetailPageViewModel : ViewModelBase
{
    private readonly IAppStateService _appState;
    private ProjectInfo? _currentProject;
    private bool _isInfoPaneOpen = true;
    private BuildingObjectInfo? _selectedBuildingObject;
    private ObservableCollection<BuildingObjectInfo> _buildingObjects = new();
    private RelayCommand? _editBuildingObjectCommand;
    private RelayCommand? _deleteBuildingObjectCommand;
    private RelayCommand? _saveBuildingObjectEditCommand;
    private RelayCommand? _cancelBuildingObjectEditCommand;
    private bool _isEditing;
    private BuildingObjectInfo? _editableBuildingObject;

    public ProjectDetailPageViewModel(IAppStateService appState)
    {
        _appState = appState ?? throw new ArgumentNullException(nameof(appState));
        _appState.CurrentProjectChanged += OnCurrentProjectChanged;

        BuildingObjects = _appState.CurrentProject?.BuildingObjects ?? new ObservableCollection<BuildingObjectInfo>();
        CurrentProject = _appState.CurrentProject;
        ReloadBuildingObjectsFromCurrentProject();

        ToggleInfoPaneCommand = new RelayCommand(_ => IsInfoPaneOpen = !IsInfoPaneOpen);
        AddBuildingObjectCommand = new RelayCommand(_ => OnAddBuildingObject());
        _editBuildingObjectCommand = new RelayCommand(_ => OnEditBuildingObject(), _ => HasSelectedBuildingObject);
        _deleteBuildingObjectCommand = new RelayCommand(_ => OnDeleteBuildingObject(), _ => HasSelectedBuildingObject && HasBuildingObjects);
        _saveBuildingObjectEditCommand = new RelayCommand(_ => OnSaveBuildingObjectEdit(), _ => IsEditing && EditableBuildingObject != null);
        _cancelBuildingObjectEditCommand = new RelayCommand(_ => OnCancelBuildingObjectEdit(), _ => IsEditing);
        EditBuildingObjectCommand = _editBuildingObjectCommand;
        DeleteBuildingObjectCommand = _deleteBuildingObjectCommand;
        SaveBuildingObjectEditCommand = _saveBuildingObjectEditCommand;
        CancelBuildingObjectEditCommand = _cancelBuildingObjectEditCommand;
    }

    public bool CanEditProject =>
        string.Equals(_appState.CurrentUser?.Role, Roles.Technik, StringComparison.OrdinalIgnoreCase);

    public ProjectInfo? CurrentProject
    {
        get => _currentProject;
        private set
        {
            if (SetProperty(ref _currentProject, value))
            {
                OnPropertyChanged(nameof(ProjectDisplayName));
                OnPropertyChanged(nameof(ProjectCode));
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(ProjectDescription));
                OnPropertyChanged(nameof(ProjectStatus));
                OnPropertyChanged(nameof(ProjectStatusText));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(CreatedAtText));
                OnPropertyChanged(nameof(UpdatedAtText));
            }
        }
    }

    public string ProjectDisplayName => CurrentProject?.DisplayName ?? string.Empty;

    public string ProjectCode => CurrentProject?.ProjectCode ?? "-";

    public string ProjectName => CurrentProject?.ProjectName ?? "-";

    public string ProjectDescription => CurrentProject?.Description ?? string.Empty;

    public string ProjectStatus => CurrentProject?.Status.ToString() ?? "N/A";

    public string ProjectStatusText => CurrentProject?.Status.ToString() ?? "-";

    public string StatusText => CurrentProject == null
        ? "Stav: -"
        : $"Stav: {ProjectStatusText}";

    public string CreatedAtText => CurrentProject?.CreatedAt.ToString("d") ?? string.Empty;

    public string UpdatedAtText => CurrentProject?.UpdatedAt.ToString("d") ?? string.Empty;

    public bool IsInfoPaneOpen
    {
        get => _isInfoPaneOpen;
        set => SetProperty(ref _isInfoPaneOpen, value);
    }

    public ObservableCollection<BuildingObjectInfo> BuildingObjects
    {
        get => _buildingObjects;
        private set
        {
            if (ReferenceEquals(_buildingObjects, value))
            {
                return;
            }

            if (_buildingObjects != null)
            {
                _buildingObjects.CollectionChanged -= OnBuildingObjectsCollectionChanged;
            }

            _buildingObjects = value ?? new ObservableCollection<BuildingObjectInfo>();
            _buildingObjects.CollectionChanged += OnBuildingObjectsCollectionChanged;
            OnPropertyChanged(nameof(BuildingObjects));
            OnPropertyChanged(nameof(TotalBuildingObjects));
            OnPropertyChanged(nameof(HasBuildingObjects));
            OnPropertyChanged(nameof(HasNoBuildingObjects));
        }
    }

    public BuildingObjectInfo? SelectedBuildingObject
    {
        get => _selectedBuildingObject;
        set
        {
            if (SetProperty(ref _selectedBuildingObject, value))
            {
                OnPropertyChanged(nameof(HasSelectedBuildingObject));
                OnPropertyChanged(nameof(HasNoSelectedBuildingObject));
                UpdateCommandStates();
                if (value == null)
                {
                    IsEditing = false;
                    EditableBuildingObject = null;
                }
                else if (IsEditing)
                {
                    IsEditing = false;
                    EditableBuildingObject = null;
                }
            }
        }
    }

    public int TotalBuildingObjects => BuildingObjects?.Count ?? 0;
    public bool HasSelectedBuildingObject => SelectedBuildingObject != null;
    public bool HasNoSelectedBuildingObject => !HasSelectedBuildingObject;
    public bool HasBuildingObjects => BuildingObjects?.Count > 0;
    public bool HasNoBuildingObjects => !HasBuildingObjects;
    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(IsNotEditing));
                UpdateCommandStates();
            }
        }
    }

    public bool IsNotEditing => !IsEditing;

    public BuildingObjectInfo? EditableBuildingObject
    {
        get => _editableBuildingObject;
        private set => SetProperty(ref _editableBuildingObject, value);
    }

    public ICommand ToggleInfoPaneCommand { get; }
    public ICommand AddBuildingObjectCommand { get; }
    public ICommand EditBuildingObjectCommand { get; }
    public ICommand DeleteBuildingObjectCommand { get; }
    public ICommand SaveBuildingObjectEditCommand { get; }
    public ICommand CancelBuildingObjectEditCommand { get; }

    private void OnCurrentProjectChanged(object? sender, ProjectInfo? project)
    {
        CurrentProject = project;
        ReloadBuildingObjectsFromCurrentProject();
    }

    private void OnAddBuildingObject()
    {
        CreateNewBuildingObject();
    }

    public void CreateNewBuildingObject()
    {
        if (_currentProject == null)
        {
            return;
        }

        var targetCollection = _currentProject.BuildingObjects;

        var newObject = new BuildingObjectInfo
        {
            Name = "Nový stavební objekt",
            Status = BuildingObjectStatus.Draft,
            HasNaip = true
        };

        targetCollection.Add(newObject);
        if (!ReferenceEquals(BuildingObjects, targetCollection))
        {
            BuildingObjects = targetCollection;
        }

        SelectedBuildingObject = newObject;
        OnPropertyChanged(nameof(TotalBuildingObjects));
        OnPropertyChanged(nameof(HasBuildingObjects));
        OnPropertyChanged(nameof(HasNoBuildingObjects));
        UpdateCommandStates();
    }

    private void OnEditBuildingObject()
    {
        if (SelectedBuildingObject == null)
        {
            return;
        }

        EditableBuildingObject = new BuildingObjectInfo
        {
            Id = SelectedBuildingObject.Id,
            Code = SelectedBuildingObject.Code,
            Name = SelectedBuildingObject.Name,
            Type = SelectedBuildingObject.Type,
            StructureType = SelectedBuildingObject.StructureType,
            CoverType = SelectedBuildingObject.CoverType,
            PrepType = SelectedBuildingObject.PrepType,
            HasNaip = SelectedBuildingObject.HasNaip,
            DeckAreaBoq = SelectedBuildingObject.DeckAreaBoq,
            DeckLength = SelectedBuildingObject.DeckLength,
            DeckWidth = SelectedBuildingObject.DeckWidth,
            RailingAreaBoq = SelectedBuildingObject.RailingAreaBoq,
            RailingLength = SelectedBuildingObject.RailingLength,
            RailingWidth = SelectedBuildingObject.RailingWidth,
            OtherAreaBoq = SelectedBuildingObject.OtherAreaBoq,
            OtherLength = SelectedBuildingObject.OtherLength,
            OtherWidth = SelectedBuildingObject.OtherWidth,
            RequiredTestCount = SelectedBuildingObject.RequiredTestCount,
            PerformedTestCount = SelectedBuildingObject.PerformedTestCount,
            Status = SelectedBuildingObject.Status,
            Notes = SelectedBuildingObject.Notes,
            HasBoqDiscrepancy = SelectedBuildingObject.HasBoqDiscrepancy,
            BoqVsRealDifferenceNote = SelectedBuildingObject.BoqVsRealDifferenceNote,
            Performances = new ObservableCollection<PerformanceEntry>(SelectedBuildingObject.Performances ?? new ObservableCollection<PerformanceEntry>())
        };

        IsEditing = true;
    }

    private void OnDeleteBuildingObject()
    {
        if (SelectedBuildingObject == null)
        {
            return;
        }

        var itemToDelete = SelectedBuildingObject;
        var removedIndex = BuildingObjects.IndexOf(itemToDelete);

        var projectCollection = _currentProject?.BuildingObjects;
        if (projectCollection != null && projectCollection.Contains(itemToDelete))
        {
            projectCollection.Remove(itemToDelete);
        }

        if (!ReferenceEquals(BuildingObjects, projectCollection) && BuildingObjects.Contains(itemToDelete))
        {
            BuildingObjects.Remove(itemToDelete);
        }

        if (BuildingObjects.Count == 0)
        {
            SelectedBuildingObject = null;
        }
        else if (removedIndex >= 0 && removedIndex < BuildingObjects.Count)
        {
            SelectedBuildingObject = BuildingObjects[removedIndex];
        }
        else
        {
            SelectedBuildingObject = BuildingObjects[^1];
        }

        OnPropertyChanged(nameof(TotalBuildingObjects));
        OnPropertyChanged(nameof(HasBuildingObjects));
        OnPropertyChanged(nameof(HasNoBuildingObjects));
        UpdateCommandStates();
    }

    private void ReloadBuildingObjectsFromCurrentProject()
    {
        var source = _currentProject?.BuildingObjects ?? new ObservableCollection<BuildingObjectInfo>();
        if (!ReferenceEquals(BuildingObjects, source))
        {
            BuildingObjects = source;
        }

        SelectedBuildingObject = BuildingObjects.Count > 0 ? BuildingObjects[0] : null;
        OnPropertyChanged(nameof(TotalBuildingObjects));
        OnPropertyChanged(nameof(HasBuildingObjects));
        OnPropertyChanged(nameof(HasNoBuildingObjects));
        UpdateCommandStates();
    }

    private void OnSaveBuildingObjectEdit()
    {
        if (SelectedBuildingObject == null || EditableBuildingObject == null)
        {
            return;
        }

        SelectedBuildingObject.Code = EditableBuildingObject.Code;
        SelectedBuildingObject.Name = EditableBuildingObject.Name;
        SelectedBuildingObject.Type = EditableBuildingObject.Type;
        SelectedBuildingObject.StructureType = EditableBuildingObject.StructureType;
        SelectedBuildingObject.CoverType = EditableBuildingObject.CoverType;
        SelectedBuildingObject.PrepType = EditableBuildingObject.PrepType;
        SelectedBuildingObject.HasNaip = EditableBuildingObject.HasNaip;
        SelectedBuildingObject.DeckAreaBoq = EditableBuildingObject.DeckAreaBoq;
        SelectedBuildingObject.DeckLength = EditableBuildingObject.DeckLength;
        SelectedBuildingObject.DeckWidth = EditableBuildingObject.DeckWidth;
        SelectedBuildingObject.RailingAreaBoq = EditableBuildingObject.RailingAreaBoq;
        SelectedBuildingObject.RailingLength = EditableBuildingObject.RailingLength;
        SelectedBuildingObject.RailingWidth = EditableBuildingObject.RailingWidth;
        SelectedBuildingObject.OtherAreaBoq = EditableBuildingObject.OtherAreaBoq;
        SelectedBuildingObject.OtherLength = EditableBuildingObject.OtherLength;
        SelectedBuildingObject.OtherWidth = EditableBuildingObject.OtherWidth;
        SelectedBuildingObject.RequiredTestCount = EditableBuildingObject.RequiredTestCount;
        SelectedBuildingObject.PerformedTestCount = EditableBuildingObject.PerformedTestCount;
        SelectedBuildingObject.Status = EditableBuildingObject.Status;
        SelectedBuildingObject.Notes = EditableBuildingObject.Notes;
        SelectedBuildingObject.HasBoqDiscrepancy = EditableBuildingObject.HasBoqDiscrepancy;
        SelectedBuildingObject.BoqVsRealDifferenceNote = EditableBuildingObject.BoqVsRealDifferenceNote;
        SelectedBuildingObject.Performances = new ObservableCollection<PerformanceEntry>(EditableBuildingObject.Performances ?? new ObservableCollection<PerformanceEntry>());

        OnPropertyChanged(nameof(SelectedBuildingObject));
        IsEditing = false;
        EditableBuildingObject = null;
        _ = PersistCurrentProjectAsync();
    }

    private void OnCancelBuildingObjectEdit()
    {
        IsEditing = false;
        EditableBuildingObject = null;
    }

    private void OnBuildingObjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(TotalBuildingObjects));
        OnPropertyChanged(nameof(HasBuildingObjects));
        OnPropertyChanged(nameof(HasNoBuildingObjects));
        if (SelectedBuildingObject == null && BuildingObjects.Count > 0)
        {
            SelectedBuildingObject = BuildingObjects[0];
        }
        UpdateCommandStates();
        _ = PersistCurrentProjectAsync();
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;
        private event EventHandler? _canExecuteChanged;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { _canExecuteChanged += value; }
            remove { _canExecuteChanged -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void UpdateCommandStates()
    {
        _editBuildingObjectCommand?.RaiseCanExecuteChanged();
        _deleteBuildingObjectCommand?.RaiseCanExecuteChanged();
        _saveBuildingObjectEditCommand?.RaiseCanExecuteChanged();
        _cancelBuildingObjectEditCommand?.RaiseCanExecuteChanged();
    }

    public async Task RefreshCurrentProjectAsync()
    {
        var registry = _appState.ProjectRegistry;
        var project = _appState.CurrentProject;
        if (project == null)
        {
            CurrentProject = null;
            BuildingObjects = new ObservableCollection<BuildingObjectInfo>();
            SelectedBuildingObject = null;
            UpdateCommandStates();
            return;
        }

        var refreshed = registry?.FindById(project.Id) ?? registry?.FindByCode(project.ProjectCode) ?? project;
        CurrentProject = refreshed;
        ReloadBuildingObjectsFromCurrentProject();
        await PersistCurrentProjectAsync();
    }

    public Task PersistCurrentProjectAsync()
    {
        var project = _appState.CurrentProject;
        if (project == null)
        {
            return Task.CompletedTask;
        }

        return _appState.ProjectRegistry.UpdateProjectAsync(project);
    }
}
