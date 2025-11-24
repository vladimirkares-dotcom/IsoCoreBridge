using System;

namespace IsoCore.Domain;

public class PerformanceEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Datum provedení prací.</summary>
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Popis činnosti (např. "Pokládka NAIP", "Brokování").</summary>
    public string Activity { get; set; } = string.Empty;

    /// <summary>Výměra provedené plochy v m² (může být 0, pokud jde jen o záznam).</summary>
    public double AreaM2 { get; set; }

    /// <summary>Volitelné poznámky.</summary>
    public string Notes { get; set; } = string.Empty;
}
