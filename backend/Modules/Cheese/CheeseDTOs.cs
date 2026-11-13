namespace TecNM.Api.Modules.Cheese;

public sealed record CheeseProduct(int Id, string Sku, string Name, string SaleUnit, decimal? UnitPrice);

public sealed record CheeseSupplier(int Id, string Name, string? Phone);

public sealed record CheeseCatalog(
    IReadOnlyList<CheeseProduct> OwnCheeses,
    IReadOnlyList<CheeseProduct> ResaleCheeses,
    IReadOnlyList<CheeseSupplier> Suppliers);

public sealed record AvailableMilkLot(int Id, string Code, DateOnly CollectionDate, decimal TotalLiters, int GoatCount);

public sealed record BatchOutput(
    int OutputId,
    int ProductId,
    string Sku,
    string ProductName,
    string SaleUnit,
    decimal Quantity,
    decimal Consumed,
    decimal Available);

public sealed record CheeseBatchItem(
    int Id,
    string Code,
    int MilkLotId,
    string MilkLotCode,
    DateOnly CollectionDate,
    decimal MilkLotLiters,
    DateOnly MadeOn,
    DateOnly ExpiresOn,
    decimal LitersUsed,
    string? Notes,
    IReadOnlyList<BatchOutput> Outputs);

public sealed record CheesePurchaseItem(
    int Id,
    string Code,
    int SupplierId,
    string SupplierName,
    int ProductId,
    string Sku,
    string ProductName,
    DateOnly PurchaseDate,
    DateOnly ExpiresOn,
    decimal Quantity,
    decimal Consumed,
    decimal Available,
    decimal UnitCost,
    decimal Total,
    string? Notes);

public sealed record StockItem(
    string Source,
    int SourceId,
    int ParentId,
    string Code,
    int ProductId,
    string Sku,
    string ProductName,
    string SaleUnit,
    DateOnly EntryDate,
    DateOnly ExpiresOn,
    int DaysToExpire,
    decimal QuantityIn,
    decimal Adjusted,
    decimal Sold,
    decimal Available);

public sealed record ProductStock(int ProductId, string Sku, string ProductName, string SaleUnit, decimal Available);

public sealed record CheeseSummary(IReadOnlyList<ProductStock> Products, int ExpiringSoon, int Expired, CheeseBatchItem? LastBatch);

public sealed record StockAdjustmentItem(
    int Id,
    DateOnly AdjustmentDate,
    string Reason,
    decimal Quantity,
    string? Notes,
    string Source,
    int SourceId,
    string Code,
    string ProductName,
    string SaleUnit);

public sealed record CreateSupplierRequest(string? Name, string? Phone);

public sealed record BatchOutputRequest(int? ProductId, decimal? Quantity);

public sealed record CreateBatchRequest(
    int? MilkLotId,
    DateOnly? MadeOn,
    DateOnly? ExpiresOn,
    decimal? LitersUsed,
    IReadOnlyList<BatchOutputRequest>? Outputs,
    string? Notes);

public sealed record UpdateBatchRequest(
    DateOnly? MadeOn,
    DateOnly? ExpiresOn,
    decimal? LitersUsed,
    IReadOnlyList<BatchOutputRequest>? Outputs,
    string? Notes);

public sealed record SavePurchaseRequest(
    int? SupplierId,
    int? ProductId,
    DateOnly? PurchaseDate,
    DateOnly? ExpiresOn,
    decimal? Quantity,
    decimal? UnitCost,
    string? Notes);

public sealed record CreateAdjustmentRequest(
    string? Source,
    int? SourceId,
    DateOnly? AdjustmentDate,
    string? Reason,
    decimal? Quantity,
    string? Notes);
