namespace TecNM.Api.Core.Entities;

/// <summary>Ordeña del día: un lote de leche por fecha con el total de litros.</summary>
public sealed class MilkLot : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateOnly CollectionDate { get; set; }
    public decimal TotalLiters { get; set; }
    public string? Notes { get; set; }

    public List<MilkRecord> Records { get; set; } = [];
}

/// <summary>Cabra ordeñada en una ordeña. No lleva litros: se mide el total del día.</summary>
public sealed class MilkRecord : AuditableEntity
{
    public int Id { get; set; }
    public int MilkLotId { get; set; }
    public int AnimalId { get; set; }
    public short SpeciesId { get; set; } = Species.GoatId;

    public MilkLot MilkLot { get; set; } = null!;
    public Animal Animal { get; set; } = null!;
}
