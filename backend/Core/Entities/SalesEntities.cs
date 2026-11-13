namespace TecNM.Api.Core.Entities;

/// <summary>Pedido de un cliente. Mapeo mínimo: lo completa el módulo Ventas.</summary>
public sealed class Order : AuditableEntity
{
    public const string Cancelled = "cancelled";

    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? EstimatedDeliveryDate { get; set; }
    public string OrderStatus { get; set; } = "quote";
}

/// <summary>Renglón de pedido: queso, animal vivo o canal. Mapeo mínimo: lo completa el módulo Ventas.</summary>
public sealed class OrderItem : AuditableEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int? CheeseBatchOutputId { get; set; }
    public int? CheesePurchaseId { get; set; }
    public int? AnimalId { get; set; }
    public int? KidSlaughterId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Order Order { get; set; } = null!;
}
