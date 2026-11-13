namespace TecNM.Api.Modules.Slaughter;

public sealed record SlaughterCandidate(
    int AnimalId,
    string VisualTag,
    string Sex,
    int? AgeMonths,
    string? ProductionStage,
    decimal? LastWeightKg,
    bool CanBeSlaughtered,
    DateOnly? MeatAllowedFrom);

public sealed record SlaughteredKid(
    int SlaughterId,
    int AnimalId,
    string VisualTag,
    string Sex,
    int? AgeMonths,
    decimal LiveWeightKg,
    decimal CarcassWeightKg,
    decimal YieldPercent,
    bool Sold);

public sealed record SlaughterRunSummary(
    int Id,
    DateOnly RunDate,
    string Slaughterhouse,
    decimal? Cost,
    int KidCount,
    decimal LiveWeightKg,
    decimal CarcassWeightKg,
    decimal YieldPercent,
    decimal? CostPerKid,
    string? Notes);

public sealed record SlaughterRunDetail(
    int Id,
    DateOnly RunDate,
    string Slaughterhouse,
    decimal? Cost,
    int KidCount,
    decimal LiveWeightKg,
    decimal CarcassWeightKg,
    decimal YieldPercent,
    decimal? CostPerKid,
    string? Notes,
    IReadOnlyList<SlaughteredKid> Kids);

public sealed record Carcass(
    int SlaughterId,
    int AnimalId,
    string VisualTag,
    int RunId,
    DateOnly RunDate,
    string Slaughterhouse,
    decimal CarcassWeightKg,
    int DaysSinceSlaughter);

public sealed record SlaughterSummary(
    int AvailableCarcasses,
    decimal AvailableCarcassKg,
    int KidsLast30Days,
    decimal? AverageYieldPercent,
    SlaughterRunSummary? LastRun);

public sealed record KidWeightsRequest(int? AnimalId, decimal? LiveWeightKg, decimal? CarcassWeightKg);

public sealed record SaveRunRequest(
    DateOnly? RunDate,
    string? Slaughterhouse,
    decimal? Cost,
    string? Notes,
    IReadOnlyList<KidWeightsRequest>? Kids);
