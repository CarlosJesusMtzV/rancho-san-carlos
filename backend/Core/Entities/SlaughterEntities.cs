namespace TecNM.Api.Core.Entities;

/// <summary>Jornada de matanza: cabritos llevados el mismo día a un rastro o matancero externo.</summary>
public sealed class SlaughterRun : AuditableEntity
{
    public int Id { get; set; }
    public DateOnly RunDate { get; set; }
    public string Slaughterhouse { get; set; } = string.Empty;
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }

    public List<KidSlaughter> Kids { get; set; } = [];
}

/// <summary>Cabrito sacrificado y pelado en una jornada; su canal se vende entera.</summary>
public sealed class KidSlaughter : AuditableEntity
{
    public int Id { get; set; }
    public int SlaughterRunId { get; set; }
    public int AnimalId { get; set; }
    public short SpeciesId { get; set; } = Species.GoatId;
    public decimal LiveWeightKg { get; set; }
    public decimal CarcassWeightKg { get; set; }

    public SlaughterRun SlaughterRun { get; set; } = null!;
    public Animal Animal { get; set; } = null!;
}
