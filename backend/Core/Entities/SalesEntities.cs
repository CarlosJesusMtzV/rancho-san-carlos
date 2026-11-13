namespace TecNM.Api.Core.Entities;

/// <summary>Tipo de cliente: restaurante o tienda, comprador de ganado o particular.</summary>
public sealed class CustomerType : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Cliente de queso o comprador de ganado.</summary>
public sealed class Customer : AuditableEntity
{
    public int Id { get; set; }
    public int CustomerTypeId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string? Rfc { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public CustomerType CustomerType { get; set; } = null!;
}

/// <summary>Pedido: venta entregada en el momento o encargo que aparta lo vendido.</summary>
public sealed class Order : AuditableEntity
{
    public const string Confirmed = "confirmed";
    public const string Delivered = "delivered";
    public const string Cancelled = "cancelled";
    public const string Pickup = "pickup";
    public const string HomeDelivery = "delivery";

    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? EstimatedDeliveryDate { get; set; }
    public string OrderStatus { get; set; } = Confirmed;
    public string DeliveryType { get; set; } = Pickup;
    public string? DeliveryAddress { get; set; }
    public DateOnly? DeliveredOn { get; set; }
    public string? Notes { get; set; }

    public Customer Customer { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = [];
}

/// <summary>Renglón de pedido: queso de una tanda o compra; animal vivo o canal por kilo (Quantity = kg).</summary>
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
    public Product Product { get; set; } = null!;
}

/// <summary>Factura opcional de un pedido entregado; el sistema no timbra.</summary>
public sealed class Invoice : AuditableEntity
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string FiscalFolio { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }
}

/// <summary>Pago completo de un pedido al entregarse.</summary>
public sealed class Payment : AuditableEntity
{
    public static readonly string[] Methods = ["cash", "transfer", "card", "check"];

    public int Id { get; set; }
    public int OrderId { get; set; }
    public DateOnly PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}
