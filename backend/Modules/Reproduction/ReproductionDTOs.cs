namespace TecNM.Api.Modules.Reproduction;

public sealed record PregnancyCheckItem(int Id, int VetVisitId, string VetName, DateOnly CheckDate, string Result, string? Notes);

public sealed record BreedingItem(
    int Id,
    short SpeciesId,
    int FemaleId,
    string FemaleTag,
    int MaleId,
    string MaleTag,
    DateOnly MatingDate,
    DateOnly ExpectedDueDate,
    string Outcome,
    string? Notes,
    IReadOnlyList<PregnancyCheckItem> Checks,
    int? BirthId);

public sealed record FemaleStatus(
    int AnimalId,
    string VisualTag,
    short SpeciesId,
    string BreedName,
    string? LotCode,
    int? AgeMonths,
    BreedingItem? Breeding,
    DateOnly? LastBirthDate,
    int BirthCount,
    int? DaysToDue);

public sealed record OffspringItem(int AnimalId, string VisualTag, string Sex, decimal? BirthWeightKg);

public sealed record BirthItem(
    int Id,
    int DamId,
    string DamTag,
    short SpeciesId,
    int? BreedingId,
    string? SireTag,
    DateOnly BirthDate,
    short OffspringCount,
    short StillbornCount,
    string? Complications,
    IReadOnlyList<OffspringItem> Offspring);

public sealed record CreateBreedingRequest(int FemaleId, int MaleId, DateOnly? MatingDate, string? Notes);

public sealed record CreateCheckRequest(int VetVisitId, string? Result, string? Notes);

public sealed record OffspringInput(string? VisualTag, string? OfficialTag, string? Sex, decimal? BirthWeightKg, int? BreedId, int? LotId);

public sealed record CreateBirthRequest(
    int DamId,
    DateOnly? BirthDate,
    short? StillbornCount,
    string? Complications,
    IReadOnlyList<OffspringInput>? Offspring);
