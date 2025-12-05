using IsoCore.Domain;

namespace IsoCore.App.ViewModels;

public class BuildingObjectDetailPageViewModel : ViewModelBase
{
    private string _code;
    private string _name;
    private BuildingStructureType _structureType;
    private BuildingCoverType _coverType;
    private BuildingObjectStatus _status;
    private string _prepType;
    private bool _hasNaip;
    private string _notes;

    public BuildingObjectDetailPageViewModel()
    {
        _code = string.Empty;
        _name = string.Empty;
        _structureType = BuildingStructureType.Unknown;
        _coverType = BuildingCoverType.Unknown;
        _status = BuildingObjectStatus.Draft;
        _prepType = string.Empty;
        _hasNaip = true;
        _notes = string.Empty;
    }

    public string Code
    {
        get => _code;
        set => SetProperty(ref _code, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public BuildingStructureType StructureType
    {
        get => _structureType;
        set => SetProperty(ref _structureType, value);
    }

    public BuildingCoverType CoverType
    {
        get => _coverType;
        set => SetProperty(ref _coverType, value);
    }

    public BuildingObjectStatus Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public string PrepType
    {
        get => _prepType;
        set => SetProperty(ref _prepType, value);
    }

    public bool HasNaip
    {
        get => _hasNaip;
        set => SetProperty(ref _hasNaip, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public void LoadFrom(BuildingObjectInfo source)
    {
        if (source == null)
        {
            return;
        }

        Code = source.Code;
        Name = source.Name;
        StructureType = source.StructureType;
        CoverType = source.CoverType;
        Status = source.Status;
        PrepType = source.PrepType.ToString();
        HasNaip = source.HasNaip;
        Notes = source.Notes;
    }

    public void ApplyTo(BuildingObjectInfo target)
    {
        if (target == null)
        {
            return;
        }

        target.Code = Code;
        target.Name = Name;
        target.StructureType = StructureType;
        target.CoverType = CoverType;
        target.Status = Status;
        if (Enum.TryParse<SurfacePrepType>(PrepType, ignoreCase: true, out var parsedPrepType))
        {
            target.PrepType = parsedPrepType;
        }
        target.HasNaip = HasNaip;
        target.Notes = Notes;
    }
}
