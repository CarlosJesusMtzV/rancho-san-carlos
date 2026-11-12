namespace TecNM.Api.Core.Entities;

/// <summary>Fila de solo lectura de la vista v_calf_sale_candidates.</summary>
public sealed class CalfSaleCandidate
{
    public int AnimalId { get; set; }
    public string VisualTag { get; set; } = string.Empty;
    public string? OfficialTag { get; set; }
    public string Sex { get; set; } = string.Empty;
    public int? LotId { get; set; }
    public DateOnly BirthDate { get; set; }
    public int AgeMonths { get; set; }
    public decimal? LastWeightKg { get; set; }
    public DateOnly? LastWeighedOn { get; set; }
    public DateOnly? MeatAllowedFrom { get; set; }
    public bool ReachedSaleTarget { get; set; }
}
