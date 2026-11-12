namespace TecNM.Api.Core.Entities;

public sealed class BreedingEvent : AuditableEntity
{
    public int Id { get; set; }
    public short SpeciesId { get; set; }
    public int FemaleId { get; set; }
    public int MaleId { get; set; }
    public DateOnly MatingDate { get; set; }
    public DateOnly ExpectedDueDate { get; set; }
    public string Outcome { get; set; } = BreedingOutcomes.Pending;
    public string? Notes { get; set; }

    public Animal Female { get; set; } = null!;
    public Animal Male { get; set; } = null!;
    public List<PregnancyCheck> Checks { get; set; } = [];
}

public static class BreedingOutcomes
{
    public const string Pending = "pending";
    public const string Pregnant = "pregnant";
    public const string Open = "open";
    public const string Aborted = "aborted";
    public const string Delivered = "delivered";

    /// <summary>Montas que todavía pueden terminar en parto.</summary>
    public static readonly string[] InProgress = [Pending, Pregnant];

    /// <summary>Resultados que puede dar el veterinario.</summary>
    public static readonly IReadOnlySet<string> CheckResults = new HashSet<string> { Pregnant, Open, Aborted };
}

public sealed class PregnancyCheck : AuditableEntity
{
    public int Id { get; set; }
    public int BreedingEventId { get; set; }
    public int VetVisitId { get; set; }
    public DateOnly CheckDate { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public BreedingEvent BreedingEvent { get; set; } = null!;
    public VetVisit VetVisit { get; set; } = null!;
}

public sealed class BirthEvent : AuditableEntity
{
    public int Id { get; set; }
    public int DamId { get; set; }
    public int? BreedingEventId { get; set; }
    public DateOnly BirthDate { get; set; }
    public short OffspringCount { get; set; }
    public short StillbornCount { get; set; }
    public string? Complications { get; set; }

    public Animal Dam { get; set; } = null!;
    public BreedingEvent? BreedingEvent { get; set; }
    public List<Animal> Offspring { get; set; } = [];
}
