namespace TecNM.Api.Core.Entities;

public sealed class Animal : AuditableEntity
{
    public int Id { get; set; }
    public string VisualTag { get; set; } = string.Empty;
    public string? OfficialTag { get; set; }
    public short SpeciesId { get; set; }
    public int BreedId { get; set; }
    public int? LotId { get; set; }
    public string Sex { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string? ProductionStage { get; set; }
    public string Origin { get; set; } = AnimalOrigins.BornOnRanch;
    public int? BirthEventId { get; set; }
    public decimal? BirthWeightKg { get; set; }
    public int? SireId { get; set; }
    public int? DamId { get; set; }
    public string? GeneticsNotes { get; set; }
    public string LifecycleStatus { get; set; } = AnimalLifecycle.Active;
    public DateOnly? ExitDate { get; set; }

    public Species Species { get; set; } = null!;
    public Breed Breed { get; set; } = null!;
    public AnimalLot? Lot { get; set; }
    public Animal? Sire { get; set; }
    public Animal? Dam { get; set; }
    public List<WeightRecord> Weights { get; set; } = [];
}

public static class AnimalLifecycle
{
    public const string Active = "active";
    public const string Sold = "sold";
    public const string Slaughtered = "slaughtered";
    public const string Dead = "dead";
    public const string Culled = "culled";
}

public static class AnimalOrigins
{
    public const string BornOnRanch = "born_on_ranch";
    public const string Purchased = "purchased";
}
