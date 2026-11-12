namespace TecNM.Api.Modules.Milking;

public sealed record MilkingGoat(int AnimalId, string VisualTag, string? ProductionStage, string? LotCode, bool CanBeMilked, DateOnly? MilkAllowedFrom);

public sealed record MilkDaySummary(int Id, string Code, DateOnly CollectionDate, decimal TotalLiters, int GoatCount, decimal LitersPerGoat, string? Notes);

public sealed record MilkedGoat(int AnimalId, string VisualTag);

public sealed record MilkDayDetail(
    int Id,
    string Code,
    DateOnly CollectionDate,
    decimal TotalLiters,
    int GoatCount,
    decimal LitersPerGoat,
    string? Notes,
    IReadOnlyList<MilkedGoat> Goats);

public sealed record MilkPeriod(int Days, decimal Liters, decimal AveragePerDay);

public sealed record MilkSummary(MilkDaySummary? LastDay, MilkPeriod Last7Days, MilkPeriod Last30Days, MilkPeriod Previous30Days);

public sealed record CreateMilkDayRequest(DateOnly? CollectionDate, decimal? TotalLiters, IReadOnlyList<int>? GoatIds, string? Notes);

public sealed record UpdateMilkDayRequest(decimal? TotalLiters, IReadOnlyList<int>? GoatIds, string? Notes);
