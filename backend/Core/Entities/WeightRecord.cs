namespace TecNM.Api.Core.Entities;

public sealed class WeightRecord : AuditableEntity
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public DateOnly WeighedOn { get; set; }
    public decimal WeightKg { get; set; }
    public string? Notes { get; set; }

    public Animal Animal { get; set; } = null!;
}
