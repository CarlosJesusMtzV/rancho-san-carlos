namespace TecNM.Api.Core.Entities;

public sealed class AnimalLot : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short SpeciesId { get; set; }
    public int? FacilityId { get; set; }

    public Species Species { get; set; } = null!;
}
