namespace TecNM.Api.Modules.AnimalHealth;

public sealed record SupplyTypeItem(int Id, string Name, string Category);

public sealed record MedicationItem(
    int Id,
    int SupplyTypeId,
    string SupplyTypeName,
    string CommercialName,
    string? ActiveIngredient,
    short DefaultMilkWithdrawalDays,
    short DefaultMeatWithdrawalDays);

public sealed record HealthCatalog(IReadOnlyList<SupplyTypeItem> SupplyTypes, IReadOnlyList<MedicationItem> Medications);

public sealed record TreatmentItem(int Id, int SupplyId, string MedicationName, string Dose, short MilkWithdrawalDays, short MeatWithdrawalDays);

public sealed record HealthEventItem(
    int Id,
    int AnimalId,
    string VisualTag,
    short SpeciesId,
    DateOnly EventDate,
    string EventType,
    string? Diagnosis,
    int? VetVisitId,
    string? VetName,
    IReadOnlyList<TreatmentItem> Treatments,
    DateOnly? MilkAllowedFrom,
    DateOnly? MeatAllowedFrom);

public sealed record HealthEventPage(IReadOnlyList<HealthEventItem> Items, int Total, int Page, int PageSize);

public sealed record CreatedEvents(int Created, IReadOnlyList<HealthEventItem> Events);

public sealed record VetVisitSummary(
    int Id,
    DateOnly VisitDate,
    string VetName,
    decimal ServiceFee,
    decimal MedicationCost,
    decimal Total,
    string? Notes,
    int EventCount,
    int AnimalCount);

public sealed record VetVisitDetail(
    int Id,
    DateOnly VisitDate,
    string VetName,
    decimal ServiceFee,
    decimal MedicationCost,
    decimal Total,
    string? Notes,
    int EventCount,
    int AnimalCount,
    IReadOnlyList<HealthEventItem> Events);

public sealed record ActiveWithdrawalItem(
    int AnimalId,
    string VisualTag,
    short SpeciesId,
    string? LotCode,
    DateOnly? MilkAllowedFrom,
    DateOnly? MeatAllowedFrom);

public sealed record LabTestItem(int Id, int AnimalId, string VisualTag, DateOnly SampleDate, string TestType, string? LabName, string Result);

public sealed record MedicationInput(
    int SupplyTypeId,
    string? CommercialName,
    string? ActiveIngredient,
    short? DefaultMilkWithdrawalDays,
    short? DefaultMeatWithdrawalDays);

public sealed record VetVisitInput(DateOnly? VisitDate, string? VetName, decimal? ServiceFee, decimal? MedicationCost, string? Notes);

public sealed record TreatmentInput(int SupplyId, string? Dose, short? MilkWithdrawalDays, short? MeatWithdrawalDays);

public sealed record CreateEventsRequest(
    IReadOnlyList<int>? AnimalIds,
    int? LotId,
    DateOnly? EventDate,
    string? EventType,
    string? Diagnosis,
    int? VetVisitId,
    IReadOnlyList<TreatmentInput>? Treatments);

public sealed record CreateLabTestRequest(int AnimalId, DateOnly? SampleDate, string? TestType, string? LabName);

public sealed record LabTestResultRequest(string? Result);

public sealed class HealthEventQuery
{
    public int? AnimalId { get; init; }
    public int? LotId { get; init; }
    public int? VisitId { get; init; }
    public string? Type { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
