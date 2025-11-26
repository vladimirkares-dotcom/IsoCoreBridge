using System;
using System.Collections.ObjectModel;

namespace IsoCore.Domain;

public class ProjectInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }

    // Backward-compatibility aliases (legacy naming)
    public string ProjectCode { get => Code; set => Code = value; }
    public string ProjectName { get => Name; set => Name = value; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Preparation;
    public ObservableCollection<BuildingObjectInfo> BuildingObjects { get; }
        = new ObservableCollection<BuildingObjectInfo>();

    public string DisplayName => string.IsNullOrWhiteSpace(Code)
        ? Name
        : $"{Code}: {Name}";

    public override string ToString() => DisplayName;
}
