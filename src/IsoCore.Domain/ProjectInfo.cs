using System;
using System.Collections.ObjectModel;

namespace IsoCore.Domain;

public class ProjectInfo
{
    /// <summary>
    /// Ozna��en�� projektu (nap�t. "D35-202").
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    public string ProjectCode { get; set; } = string.Empty;

    /// <summary>
    /// N��zev projektu (nap�t. "Most na D35 v km 40,605").
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    public ProjectStatus Status { get; set; } = ProjectStatus.Preparation;

    /// <summary>
    /// Stavebn�� objekty (SO 202, SO 207, ...), kter�c pat�t�� k t�cto stavb�>.
    /// UI je zat��m nevyu����v�� �?" p�tipraveno pro dal���� krok.
    /// </summary>
    public ObservableCollection<BuildingObjectInfo> BuildingObjects { get; }
        = new ObservableCollection<BuildingObjectInfo>();

    /// <summary>
    /// Zobrazen�� n��zev v UI.
    /// Nap�t. "SO 202: N��chod obchvat".
    /// </summary>
    public string DisplayName => string.IsNullOrWhiteSpace(ProjectCode)
        ? ProjectName
        : $"{ProjectCode}: {ProjectName}";

    public override string ToString() => DisplayName;
}
