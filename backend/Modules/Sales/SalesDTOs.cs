namespace TecNM.Api.Modules.Sales;

public sealed record SalesProduct(int Id, string Sku, string Name, string Kind, string SaleUnit, decimal? UnitPrice);

public sealed record CustomerTypeItem(int Id, string Name);

public sealed record SalesCatalog(IReadOnlyList<SalesProduct> Products, IReadOnlyList<CustomerTypeItem> CustomerTypes);

public sealed record CustomerItem(int Id, string Name, int CustomerTypeId, string CustomerTypeName, string? Rfc, string? Phone, string? Email, string? Address);

public sealed record AvailableCheese(string Source, int SourceId, string Code, int ProductId, string ProductName, string SaleUnit, decimal Available, DateOnly ExpiresOn, decimal? UnitPrice);

public sealed record AvailableAnimal(int AnimalId, string VisualTag, int ProductId, string ProductName, string Sex, int? AgeMonths, decimal? LastWeightKg, DateOnly? MeatAllowedFrom);

public sealed record AvailableCarcass(int SlaughterId, string VisualTag, int ProductId, decimal CarcassWeightKg, DateOnly RunDate);

public sealed record AvailableItems(IReadOnlyList<AvailableCheese> Cheese, IReadOnlyList<AvailableAnimal> Animals, IReadOnlyList<AvailableCarcass> Carcasses);

public sealed record OrderLine(
    int Id,
    string Kind,
    int ProductId,
    string ProductName,
    string SaleUnit,
    string? Source,
    int SourceId,
    string Label,
    decimal Quantity,
    decimal UnitPrice,
    decimal Total);

public sealed record OrderSummary(
    int Id,
    int CustomerId,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly? EstimatedDeliveryDate,
    string Status,
    string DeliveryType,
    DateOnly? DeliveredOn,
    int ItemCount,
    decimal Total,
    string? InvoiceFolio);

public sealed record PaymentItem(DateOnly Date, decimal Amount, string Method);

public sealed record InvoiceItem(string FiscalFolio, DateOnly IssueDate, decimal Subtotal, decimal Taxes, decimal Total);

public sealed record OrderDetail(
    int Id,
    int CustomerId,
    string CustomerName,
    DateOnly OrderDate,
    DateOnly? EstimatedDeliveryDate,
    string Status,
    string DeliveryType,
    DateOnly? DeliveredOn,
    int ItemCount,
    decimal Total,
    string? InvoiceFolio,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<OrderLine> Items,
    PaymentItem? Payment,
    InvoiceItem? Invoice);

public sealed record SalesByKind(decimal Cheese, decimal Animal, decimal Carcass);

public sealed record SalesSummary(decimal MonthTotal, SalesByKind MonthByKind, int PendingOrders, int DueSoon);

public sealed record SaveCustomerRequest(string? Name, int? CustomerTypeId, string? Rfc, string? Phone, string? Email, string? Address);

public sealed record OrderLineRequest(string? Kind, string? Source, int? SourceId, decimal? Quantity, decimal? UnitPrice);

public sealed record DeliverNowRequest(string? PaymentMethod);

public sealed record SaveOrderRequest(
    int? CustomerId,
    DateOnly? OrderDate,
    DateOnly? EstimatedDeliveryDate,
    string? DeliveryType,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<OrderLineRequest>? Items,
    DeliverNowRequest? DeliverNow);

public sealed record DeliverOrderRequest(DateOnly? DeliveredOn, string? PaymentMethod);

public sealed record SaveInvoiceRequest(string? FiscalFolio, DateOnly? IssueDate, decimal? Taxes);
