using System;
using System.Collections.ObjectModel;

namespace IsoCore.Domain;

public class BuildingObjectInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }

    public ObservableCollection<PerformanceEntry> Performances { get; set; }
        = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Code)
            ? Name ?? string.Empty
            : $"{Code}: {Name}";

    public CalcProfile CalcProfile { get; set; } = new();
}
