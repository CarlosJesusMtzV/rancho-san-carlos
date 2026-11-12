namespace TecNM.Api.Core.Entities;

public sealed class SupplyType : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public static class SupplyCategories
{
    public const string Medicine = "medicine";
    public const string Vaccine = "vaccine";
}

/// <summary>Insumo. En sanidad funciona como catálogo de medicamentos, sin existencias.</summary>
public sealed class Supply : AuditableEntity
{
    public int Id { get; set; }
    public int SupplyTypeId { get; set; }
    public string CommercialName { get; set; } = string.Empty;
    public string? ActiveIngredient { get; set; }
    public short DefaultMilkWithdrawalDays { get; set; }
    public short DefaultMeatWithdrawalDays { get; set; }

    public SupplyType SupplyType { get; set; } = null!;
}

public sealed class VetVisit : AuditableEntity
{
    public int Id { get; set; }
    public DateOnly VisitDate { get; set; }
    public string VetName { get; set; } = string.Empty;
    public decimal ServiceFee { get; set; }
    public decimal MedicationCost { get; set; }
    public string? Notes { get; set; }

    public List<HealthEvent> Events { get; set; } = [];
}

public sealed class HealthEvent : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public DateOnly EventDate { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public int? VetVisitId { get; set; }

    public Animal Animal { get; set; } = null!;
    public VetVisit? VetVisit { get; set; }
    public List<Treatment> Treatments { get; set; } = [];
}

public static class HealthEventTypes
{
    public const string Vaccination = "vaccination";
    public const string Deworming = "deworming";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string> { Vaccination, Deworming, "illness", "injury", "checkup", "other" };
}

public sealed class Treatment : AuditableEntity
{
    public int Id { get; set; }
    public int HealthEventId { get; set; }
    public int SupplyId { get; set; }
    public string Dose { get; set; } = string.Empty;
    public short MilkWithdrawalDays { get; set; }
    public short MeatWithdrawalDays { get; set; }

    public HealthEvent HealthEvent { get; set; } = null!;
    public Supply Supply { get; set; } = null!;
}

public sealed class LabTest : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public DateOnly SampleDate { get; set; }
    public string TestType { get; set; } = string.Empty;
    public string Result { get; set; } = LabResults.Pending;
    public string? LabName { get; set; }

    public Animal Animal { get; set; } = null!;
}

public static class LabResults
{
    public const string Pending = "pending";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Pending, "negative", "positive", "suspect" };
}

/// <summary>Fila de solo lectura de la vista v_active_withdrawals.</summary>
public sealed class ActiveWithdrawal
{
    public int AnimalId { get; set; }
    public string VisualTag { get; set; } = string.Empty;
    public short SpeciesId { get; set; }
    public DateOnly? MilkAllowedFrom { get; set; }
    public DateOnly? MeatAllowedFrom { get; set; }
}
