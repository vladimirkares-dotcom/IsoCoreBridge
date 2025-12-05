namespace IsoCore.Domain;

public enum BuildingObjectEditMode
{
    None = 0,
    New = 1,
    Edit = 2
}

public sealed class BuildingObjectEditSession
{
    public BuildingObjectEditMode Mode { get; init; }

    // Project that owns the building object being edited.
    public ProjectInfo? Project { get; init; }

    // Target object for editing. For "New" this may be null;
    // for "Edit" this is the existing BuildingObjectInfo instance.
    public BuildingObjectInfo? Target { get; init; }
}
