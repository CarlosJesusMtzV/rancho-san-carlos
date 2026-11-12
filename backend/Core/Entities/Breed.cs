namespace TecNM.Api.Core.Entities;

public sealed class Breed : AuditableEntity
{
    public int Id { get; set; }
    public short SpeciesId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Purpose { get; set; }

    public Species Species { get; set; } = null!;
}
