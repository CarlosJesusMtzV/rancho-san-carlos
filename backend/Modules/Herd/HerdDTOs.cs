namespace TecNM.Api.Modules.Herd;

public sealed record SpeciesItem(short Id, string Name, string ProductionPurpose);

public sealed record BreedItem(int Id, short SpeciesId, string Name, string? Purpose);

public sealed record LotItem(int Id, short SpeciesId, string Code, string? Description, int ActiveAnimals);

public sealed record HerdCatalog(IReadOnlyList<SpeciesItem> Species, IReadOnlyList<BreedItem> Breeds, IReadOnlyList<LotItem> Lots);

public sealed record AnimalRef(int Id, string VisualTag);

public sealed record WeightItem(int Id, DateOnly WeighedOn, decimal WeightKg, string? Notes);

public sealed record AnimalSummary(
    int Id,
    string VisualTag,
    string? OfficialTag,
    short SpeciesId,
    string SpeciesName,
    string BreedName,
    int? LotId,
    string? LotCode,
    string Sex,
    DateOnly? BirthDate,
    int? AgeMonths,
    string? ProductionStage,
    string LifecycleStatus,
    decimal? LastWeightKg,
    DateOnly? LastWeighedOn);

public sealed record AnimalPage(IReadOnlyList<AnimalSummary> Items, int Total, int Page, int PageSize);

public sealed record AnimalDetail(
    int Id,
    string VisualTag,
    string? OfficialTag,
    short SpeciesId,
    string SpeciesName,
    int BreedId,
    string BreedName,
    int? LotId,
    string? LotCode,
    string Sex,
    DateOnly? BirthDate,
    int? AgeMonths,
    string? ProductionStage,
    string Origin,
    decimal? BirthWeightKg,
    string LifecycleStatus,
    DateOnly? ExitDate,
    string? GeneticsNotes,
    AnimalRef? Sire,
    AnimalRef? Dam,
    decimal? LastWeightKg,
    DateOnly? LastWeighedOn,
    IReadOnlyList<WeightItem> Weights);

public sealed record CalfSaleCandidateItem(
    int AnimalId,
    string VisualTag,
    string? OfficialTag,
    string Sex,
    string? LotCode,
    DateOnly BirthDate,
    int AgeMonths,
    decimal? LastWeightKg,
    DateOnly? LastWeighedOn,
    DateOnly? MeatAllowedFrom,
    bool ReachedSaleTarget);

public sealed class AnimalQuery
{
    public short? SpeciesId { get; init; }
    public int? LotId { get; init; }
    public string? Sex { get; init; }
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed record CreateAnimalRequest(
    string? VisualTag,
    string? OfficialTag,
    short SpeciesId,
    int BreedId,
    int? LotId,
    string? Sex,
    DateOnly? BirthDate,
    string? ProductionStage,
    string? Origin,
    decimal? BirthWeightKg,
    int? SireId,
    int? DamId,
    string? GeneticsNotes);

public sealed record UpdateAnimalRequest(
    string? VisualTag,
    string? OfficialTag,
    int BreedId,
    int? LotId,
    string? Sex,
    DateOnly? BirthDate,
    string? ProductionStage,
    string? Origin,
    decimal? BirthWeightKg,
    int? SireId,
    int? DamId,
    string? GeneticsNotes);

public sealed record AnimalExitRequest(string? Status, DateOnly? ExitDate);

public sealed record CreateWeightRequest(DateOnly? WeighedOn, decimal? WeightKg, string? Notes);

public sealed record CreateBreedRequest(short SpeciesId, string? Name, string? Purpose);

public sealed record CreateLotRequest(short SpeciesId, string? Code, string? Description);
