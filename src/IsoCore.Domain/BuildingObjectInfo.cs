using System;
using System.Collections.ObjectModel;

namespace IsoCore.Domain;

public class BuildingObjectInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }

    public BuildingStructureType StructureType { get; set; }
    public BuildingCoverType CoverType { get; set; }
    public SurfacePrepType PrepType { get; set; }
    public bool HasNaip { get; set; } = true;

    public double? DeckAreaBoq { get; set; }
    public double? DeckLength { get; set; }
    public double? DeckWidth { get; set; }

    public double? RailingAreaBoq { get; set; }
    public double? RailingLength { get; set; }
    public double? RailingWidth { get; set; }

    public double? OtherAreaBoq { get; set; }
    public double? OtherLength { get; set; }
    public double? OtherWidth { get; set; }

    public int? RequiredTestCount { get; set; }
    public int? PerformedTestCount { get; set; }

    public BuildingObjectStatus Status { get; set; } = BuildingObjectStatus.Draft;
    public string? Notes { get; set; }

    public bool HasBoqDiscrepancy { get; set; }
    public string? BoqVsRealDifferenceNote { get; set; }

    public ObservableCollection<PerformanceEntry> Performances { get; set; }
        = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Code)
            ? Name ?? string.Empty
            : $"{Code}: {Name}";

    public CalcProfile CalcProfile { get; set; } = new();
}
