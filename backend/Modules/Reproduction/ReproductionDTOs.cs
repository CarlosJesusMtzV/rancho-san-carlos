namespace TecNM.Api.Modules.Reproduction;

public sealed record PregnancyCheckItem(int Id, int VetVisitId, string VetName, DateOnly CheckDate, string Result, string? Notes);

public sealed record BreedingItem(
    int Id,
    int SeasonId,
    short SpeciesId,
    int FemaleId,
    string FemaleTag,
    int MaleId,
    string MaleTag,
    DateOnly StartDate,
    DateOnly ExpectedDueDate,
    string Outcome,
    string? Notes,
    IReadOnlyList<PregnancyCheckItem> Checks,
    int? BirthId);

public sealed record EmpadreItem(
    int SeasonId,
    short SpeciesId,
    int MaleId,
    string MaleTag,
    DateOnly StartDate,
    string? Notes,
    IReadOnlyList<BreedingItem> Females);

public sealed record FemaleStatus(
    int AnimalId,
    string VisualTag,
    short SpeciesId,
    string BreedName,
    string? Situation,
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

public sealed record CreateEmpadreRequest(int MaleId, IReadOnlyList<int>? FemaleIds, DateOnly? StartDate, string? Notes);

public sealed record CreateCheckRequest(int VetVisitId, string? Result, string? Notes);

public sealed record OffspringInput(string? VisualTag, string? OfficialTag, string? Sex, decimal? BirthWeightKg, int? BreedId, int? LotId);

public sealed record CreateBirthRequest(
    int DamId,
    DateOnly? BirthDate,
    short? StillbornCount,
    string? Complications,
    IReadOnlyList<OffspringInput>? Offspring);
