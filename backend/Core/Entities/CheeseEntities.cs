namespace TecNM.Api.Core.Entities;

/// <summary>Tipo de producto: queso propio, queso de reventa, ganado en pie o canal.</summary>
public sealed class ProductType : AuditableEntity
{
    public const string OwnCheese = "cheese";
    public const string ResaleCheese = "resale";

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

/// <summary>Producto del catálogo de venta. <see cref="SpeciesId"/> es la especie de origen (leche o animal).</summary>
public sealed class Product : AuditableEntity
{
    public const string UnitKg = "kg";
    public const string UnitPiece = "piece";

    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int ProductTypeId { get; set; }
    public short SpeciesId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SaleUnit { get; set; } = string.Empty;
    public decimal? UnitPrice { get; set; }

    public ProductType ProductType { get; set; } = null!;
}

/// <summary>Proveedor de insumos o de queso de reventa.</summary>
public sealed class Supplier : AuditableEntity
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? Rfc { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>Tanda de queso fresco de cabra (canasta y panela) con la leche de un solo día.</summary>
public sealed class CheeseBatch : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int MilkLotId { get; set; }
    public DateOnly MadeOn { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public decimal LitersUsed { get; set; }
    public string? Notes { get; set; }

    public MilkLot MilkLot { get; set; } = null!;
    public List<CheeseBatchOutput> Outputs { get; set; } = [];
}

/// <summary>Lo que salió de una tanda en una presentación: kg de canasta o piezas de panela.</summary>
public sealed class CheeseBatchOutput : AuditableEntity
{
    public int Id { get; set; }
    public int CheeseBatchId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }

    public CheeseBatch CheeseBatch { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Compra de queso hecho para reventa (asadero de leche de vaca).</summary>
public sealed class CheesePurchase : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int ProductId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }

    public Supplier Supplier { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

/// <summary>Salida de queso que no es venta: merma, consumo propio o regalo.</summary>
public sealed class CheeseStockAdjustment : AuditableEntity
{
    public static readonly string[] Reasons = ["waste", "own_use", "gift"];

    public int Id { get; set; }
    public int? CheeseBatchOutputId { get; set; }
    public int? CheesePurchaseId { get; set; }
    public DateOnly AdjustmentDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Fila de la vista v_cheese_stock: existencia de una presentación de tanda o de una compra.</summary>
public sealed class CheeseStock
{
    public const string FromBatch = "batch";
    public const string FromPurchase = "purchase";

    public string Source { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public int ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SaleUnit { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public decimal QuantityIn { get; set; }
    public decimal Adjusted { get; set; }
    public decimal Sold { get; set; }
    public decimal Available { get; set; }
}
