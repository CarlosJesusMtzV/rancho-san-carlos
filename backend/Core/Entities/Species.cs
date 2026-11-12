namespace TecNM.Api.Core.Entities;

public sealed class Species : AuditableEntity
{
    public const short CattleId = 1;
    public const short GoatId = 2;

    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProductionPurpose { get; set; } = string.Empty;
    public string? Description { get; set; }
}
