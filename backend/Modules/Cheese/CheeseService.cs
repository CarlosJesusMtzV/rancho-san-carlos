using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Cheese;

public sealed class CheeseService(TecNMDbContext db)
{
    private const int DefaultRangeDays = 90;
    private const int MilkLotWindowDays = 30;
    private const int BatchShelfLifeDays = 7;
    private const int BatchMaxShelfLifeDays = 11;
    private const int PurchaseShelfLifeDays = 15;
    private const int ExpiringSoonDays = 2;
    private const decimal MaxQuantity = 10_000m;
    private const decimal MaxUnitCost = 100_000m;

    // ------------------------------------------------------------------ Catálogo y proveedores

    public async Task<CheeseCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var products = await CheeseProducts().ToListAsync(cancellationToken);
        var suppliers = await db.Suppliers.AsNoTracking()
            .OrderBy(supplier => supplier.CompanyName)
            .Select(supplier => new CheeseSupplier(supplier.Id, supplier.CompanyName, supplier.Phone))
            .ToListAsync(cancellationToken);
        return new CheeseCatalog(
            products.Where(product => product.Kind == ProductType.OwnCheese).Select(product => product.Item).ToList(),
            products.Where(product => product.Kind == ProductType.ResaleCheese).Select(product => product.Item).ToList(),
            suppliers);
    }

    public async Task<CheeseSupplier> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken)
    {
        var name = TextInput.Clean(request.Name) ?? throw ApiException.BadRequest("Escribe el nombre del proveedor.");
        if (name.Length > 160)
        {
            throw ApiException.BadRequest("El nombre del proveedor admite hasta 160 caracteres.");
        }

        var phone = TextInput.Clean(request.Phone);
        if (phone?.Length > 20)
        {
            throw ApiException.BadRequest("El teléfono admite hasta 20 caracteres.");
        }

        var lowered = name.ToLower();
        if (await db.Suppliers.AnyAsync(supplier => supplier.CompanyName.ToLower() == lowered, cancellationToken))
        {
            throw ApiException.Conflict($"Ya existe el proveedor {name}.");
        }

        var supplier = new Supplier { CompanyName = name, Phone = phone };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(cancellationToken);
        return new CheeseSupplier(supplier.Id, supplier.CompanyName, supplier.Phone);
    }

    // ------------------------------------------------------------------ Tandas

    public async Task<IReadOnlyList<AvailableMilkLot>> ListAvailableMilkLotsAsync(CancellationToken cancellationToken)
    {
        var since = RanchClock.Today.AddDays(-MilkLotWindowDays);
        return await db.MilkLots.AsNoTracking()
            .Where(lot => lot.CollectionDate >= since && !db.CheeseBatches.Any(batch => batch.MilkLotId == lot.Id))
            .OrderByDescending(lot => lot.CollectionDate)
            .Select(lot => new AvailableMilkLot(lot.Id, lot.Code, lot.CollectionDate, lot.TotalLiters, lot.Records.Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CheeseBatchItem>> ListBatchesAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var (start, end) = Range(from, to);
        var batches = await BatchQuery()
            .Where(batch => batch.MadeOn >= start && batch.MadeOn <= end)
            .OrderByDescending(batch => batch.MadeOn).ThenByDescending(batch => batch.Id)
            .ToListAsync(cancellationToken);
        return await ToBatchItemsAsync(batches, cancellationToken);
    }

    public async Task<CheeseBatchItem> GetBatchAsync(int id, CancellationToken cancellationToken)
    {
        var batch = await BatchQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La tanda no existe.");
        return (await ToBatchItemsAsync([batch], cancellationToken))[0];
    }

    public async Task<CheeseBatchItem> CreateBatchAsync(CreateBatchRequest request, CancellationToken cancellationToken)
    {
        var milkLotId = request.MilkLotId ?? throw ApiException.BadRequest("Elige la ordeña de la que sale la tanda.");
        var lot = await db.MilkLots.AsNoTracking().FirstOrDefaultAsync(item => item.Id == milkLotId, cancellationToken)
            ?? throw ApiException.NotFound("La ordeña no existe.");
        var existing = await db.CheeseBatches.AsNoTracking()
            .Where(batch => batch.MilkLotId == milkLotId)
            .Select(batch => batch.Code)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            throw ApiException.Conflict($"La leche del {Day(lot.CollectionDate)} ya se usó en la tanda {existing}.");
        }

        var madeOn = request.MadeOn ?? lot.CollectionDate;
        var (liters, expiresOn) = ValidateBatchFields(lot, madeOn, request.ExpiresOn, request.LitersUsed ?? lot.TotalLiters);
        var outputs = await ValidateOutputsAsync(request.Outputs, cancellationToken);

        var batch = new CheeseBatch
        {
            Code = await NextCodeAsync("QF", lot.CollectionDate, true, cancellationToken),
            MilkLotId = lot.Id,
            MadeOn = madeOn,
            ExpiresOn = expiresOn,
            LitersUsed = liters,
            Notes = CleanNotes(request.Notes),
            Outputs = outputs.Select(output => new CheeseBatchOutput { ProductId = output.ProductId, Quantity = output.Quantity }).ToList(),
        };
        db.CheeseBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batch.Id, cancellationToken);
    }

    public async Task<CheeseBatchItem> UpdateBatchAsync(int id, UpdateBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await db.CheeseBatches
            .Include(item => item.MilkLot)
            .Include(item => item.Outputs).ThenInclude(output => output.Product)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La tanda no existe.");

        var madeOn = request.MadeOn ?? throw ApiException.BadRequest("Indica la fecha de elaboración.");
        var (liters, expiresOn) = ValidateBatchFields(batch.MilkLot, madeOn, request.ExpiresOn ?? throw ApiException.BadRequest("Indica la caducidad."), request.LitersUsed ?? throw ApiException.BadRequest("Indica los litros usados."));
        var outputs = await ValidateOutputsAsync(request.Outputs, cancellationToken);

        var consumed = await ConsumedAsync(CheeseStock.FromBatch, batch.Outputs.Select(output => output.Id).ToList(), cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var current in batch.Outputs.ToList())
        {
            var used = consumed.GetValueOrDefault(current.Id);
            var index = outputs.FindIndex(output => output.ProductId == current.ProductId);
            if (index < 0)
            {
                if (used > 0)
                {
                    throw ApiException.Unprocessable($"De {current.Product.Name} ya salieron {Quantity(used, current.Product.SaleUnit)}; no se puede quitar de la tanda.");
                }

                current.DeletedAt = now;
                current.Status = 0;
            }
            else
            {
                if (outputs[index].Quantity < used)
                {
                    throw ApiException.Unprocessable($"De {current.Product.Name} ya salieron {Quantity(used, current.Product.SaleUnit)}; la tanda no puede quedar en menos.");
                }

                current.Quantity = outputs[index].Quantity;
            }
        }

        var currentProducts = batch.Outputs.Select(output => output.ProductId).ToHashSet();
        batch.Outputs.AddRange(outputs
            .Where(output => !currentProducts.Contains(output.ProductId))
            .Select(output => new CheeseBatchOutput { ProductId = output.ProductId, Quantity = output.Quantity }));
        batch.MadeOn = madeOn;
        batch.ExpiresOn = expiresOn;
        batch.LitersUsed = liters;
        batch.Notes = CleanNotes(request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return await GetBatchAsync(batch.Id, cancellationToken);
    }

    public async Task DeleteBatchAsync(int id, CancellationToken cancellationToken)
    {
        var batch = await db.CheeseBatches.Include(item => item.Outputs)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La tanda no existe.");

        var consumed = await db.CheeseStock.AsNoTracking()
            .Where(stock => stock.Source == CheeseStock.FromBatch && stock.ParentId == id)
            .Select(stock => new { stock.Adjusted, stock.Sold })
            .ToListAsync(cancellationToken);
        if (consumed.Any(stock => stock.Sold > 0))
        {
            throw ApiException.Conflict($"La tanda {batch.Code} ya tiene ventas; no se puede eliminar.");
        }

        if (consumed.Any(stock => stock.Adjusted > 0))
        {
            throw ApiException.Conflict($"La tanda {batch.Code} tiene mermas registradas; elimínalas primero en el historial.");
        }

        var now = DateTime.UtcNow;
        foreach (var output in batch.Outputs)
        {
            output.DeletedAt = now;
            output.Status = 0;
        }

        batch.DeletedAt = now;
        batch.Status = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ Compras de reventa

    public async Task<IReadOnlyList<CheesePurchaseItem>> ListPurchasesAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var (start, end) = Range(from, to);
        var purchases = await PurchaseQuery()
            .Where(purchase => purchase.PurchaseDate >= start && purchase.PurchaseDate <= end)
            .OrderByDescending(purchase => purchase.PurchaseDate).ThenByDescending(purchase => purchase.Id)
            .ToListAsync(cancellationToken);
        return await ToPurchaseItemsAsync(purchases, cancellationToken);
    }

    public async Task<CheesePurchaseItem> GetPurchaseAsync(int id, CancellationToken cancellationToken)
    {
        var purchase = await PurchaseQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La compra no existe.");
        return (await ToPurchaseItemsAsync([purchase], cancellationToken))[0];
    }

    public async Task<CheesePurchaseItem> CreatePurchaseAsync(SavePurchaseRequest request, CancellationToken cancellationToken)
    {
        var fields = await ValidatePurchaseAsync(request, null, cancellationToken);
        var purchase = new CheesePurchase
        {
            Code = await NextCodeAsync("CQ", fields.PurchaseDate, false, cancellationToken),
            SupplierId = fields.SupplierId,
            ProductId = fields.ProductId,
            PurchaseDate = fields.PurchaseDate,
            ExpiresOn = fields.ExpiresOn,
            Quantity = fields.Quantity,
            UnitCost = fields.UnitCost,
            Notes = fields.Notes,
        };
        db.CheesePurchases.Add(purchase);
        await db.SaveChangesAsync(cancellationToken);
        return await GetPurchaseAsync(purchase.Id, cancellationToken);
    }

    public async Task<CheesePurchaseItem> UpdatePurchaseAsync(int id, SavePurchaseRequest request, CancellationToken cancellationToken)
    {
        var purchase = await db.CheesePurchases.Include(item => item.Product)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La compra no existe.");
        var fields = await ValidatePurchaseAsync(request, purchase, cancellationToken);

        var used = (await ConsumedAsync(CheeseStock.FromPurchase, [purchase.Id], cancellationToken)).GetValueOrDefault(purchase.Id);
        if (fields.Quantity < used)
        {
            throw ApiException.Unprocessable($"De la compra {purchase.Code} ya salieron {Quantity(used, purchase.Product.SaleUnit)}; no puede quedar en menos.");
        }

        purchase.SupplierId = fields.SupplierId;
        purchase.PurchaseDate = fields.PurchaseDate;
        purchase.ExpiresOn = fields.ExpiresOn;
        purchase.Quantity = fields.Quantity;
        purchase.UnitCost = fields.UnitCost;
        purchase.Notes = fields.Notes;
        await db.SaveChangesAsync(cancellationToken);
        return await GetPurchaseAsync(purchase.Id, cancellationToken);
    }

    public async Task DeletePurchaseAsync(int id, CancellationToken cancellationToken)
    {
        var purchase = await db.CheesePurchases.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La compra no existe.");
        var stock = await db.CheeseStock.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Source == CheeseStock.FromPurchase && item.SourceId == id, cancellationToken);
        if (stock?.Sold > 0)
        {
            throw ApiException.Conflict($"La compra {purchase.Code} ya tiene ventas; no se puede eliminar.");
        }

        if (stock?.Adjusted > 0)
        {
            throw ApiException.Conflict($"La compra {purchase.Code} tiene mermas registradas; elimínalas primero en el historial.");
        }

        purchase.DeletedAt = DateTime.UtcNow;
        purchase.Status = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ Existencias y mermas

    public async Task<IReadOnlyList<StockItem>> ListStockAsync(bool all, CancellationToken cancellationToken)
    {
        var since = RanchClock.Today.AddDays(-DefaultRangeDays);
        var query = db.CheeseStock.AsNoTracking();
        query = all ? query.Where(stock => stock.Available > 0 || stock.EntryDate >= since) : query.Where(stock => stock.Available > 0);
        var rows = await query.OrderBy(stock => stock.ExpiresOn).ThenBy(stock => stock.Code).ThenBy(stock => stock.Sku)
            .ToListAsync(cancellationToken);
        return rows.Select(ToStockItem).ToList();
    }

    public async Task<CheeseSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var products = await CheeseProducts().ToListAsync(cancellationToken);
        var stock = await db.CheeseStock.AsNoTracking().Where(item => item.Available > 0).ToListAsync(cancellationToken);
        var today = RanchClock.Today;
        var totals = products
            .OrderBy(product => product.Kind == ProductType.ResaleCheese).ThenBy(product => product.Item.Sku)
            .Select(product => new ProductStock(
                product.Item.Id,
                product.Item.Sku,
                product.Item.Name,
                product.Item.SaleUnit,
                stock.Where(item => item.ProductId == product.Item.Id).Sum(item => item.Available)))
            .ToList();
        var days = stock.Select(item => item.ExpiresOn.DayNumber - today.DayNumber).ToList();

        var lastId = await db.CheeseBatches.AsNoTracking()
            .OrderByDescending(batch => batch.MadeOn).ThenByDescending(batch => batch.Id)
            .Select(batch => (int?)batch.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var lastBatch = lastId is null ? null : await GetBatchAsync(lastId.Value, cancellationToken);
        return new CheeseSummary(totals, days.Count(day => day is >= 0 and <= ExpiringSoonDays), days.Count(day => day < 0), lastBatch);
    }

    public async Task<IReadOnlyList<StockAdjustmentItem>> ListAdjustmentsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var (start, end) = Range(from, to);
        var adjustments = await db.CheeseStockAdjustments.AsNoTracking()
            .Where(adjustment => adjustment.AdjustmentDate >= start && adjustment.AdjustmentDate <= end)
            .OrderByDescending(adjustment => adjustment.AdjustmentDate).ThenByDescending(adjustment => adjustment.Id)
            .ToListAsync(cancellationToken);
        return await ToAdjustmentItemsAsync(adjustments, cancellationToken);
    }

    public async Task<StockAdjustmentItem> CreateAdjustmentAsync(CreateAdjustmentRequest request, CancellationToken cancellationToken)
    {
        var source = request.Source?.Trim().ToLowerInvariant();
        if (source is not (CheeseStock.FromBatch or CheeseStock.FromPurchase))
        {
            throw ApiException.BadRequest("Indica si la merma sale de una tanda o de una compra.");
        }

        var sourceId = request.SourceId ?? throw ApiException.BadRequest("Elige de qué queso sale la merma.");
        var reason = request.Reason?.Trim().ToLowerInvariant();
        if (reason is null || !CheeseStockAdjustment.Reasons.Contains(reason))
        {
            throw ApiException.BadRequest("El motivo debe ser merma, consumo propio o regalo.");
        }

        var stock = await db.CheeseStock.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Source == source && item.SourceId == sourceId, cancellationToken)
            ?? throw ApiException.NotFound("No se encontró ese queso en existencia.");

        var date = request.AdjustmentDate ?? RanchClock.Today;
        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de la merma no puede ser futura.");
        }

        if (date < stock.EntryDate)
        {
            throw ApiException.Unprocessable($"La merma no puede ser anterior al {Day(stock.EntryDate)}, cuando entró el queso {stock.Code}.");
        }

        var quantity = ValidateQuantity(request.Quantity, stock.SaleUnit, stock.ProductName, "Indica cuánto queso sale.");
        if (quantity > stock.Available)
        {
            throw ApiException.Unprocessable($"Solo hay {Quantity(stock.Available, stock.SaleUnit)} de {stock.ProductName} ({stock.Code}).");
        }

        var adjustment = new CheeseStockAdjustment
        {
            CheeseBatchOutputId = source == CheeseStock.FromBatch ? sourceId : null,
            CheesePurchaseId = source == CheeseStock.FromPurchase ? sourceId : null,
            AdjustmentDate = date,
            Reason = reason,
            Quantity = quantity,
            Notes = CleanNotes(request.Notes),
        };
        db.CheeseStockAdjustments.Add(adjustment);
        await db.SaveChangesAsync(cancellationToken);
        return (await ToAdjustmentItemsAsync([adjustment], cancellationToken))[0];
    }

    public async Task DeleteAdjustmentAsync(int id, CancellationToken cancellationToken)
    {
        var adjustment = await db.CheeseStockAdjustments.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La merma no existe.");
        adjustment.DeletedAt = DateTime.UtcNow;
        adjustment.Status = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ Validación

    private static (decimal Liters, DateOnly ExpiresOn) ValidateBatchFields(MilkLot lot, DateOnly madeOn, DateOnly? expiresOn, decimal liters)
    {
        if (madeOn > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de elaboración no puede ser futura.");
        }

        if (madeOn < lot.CollectionDate)
        {
            throw ApiException.Unprocessable($"La tanda no puede elaborarse antes de la ordeña del {Day(lot.CollectionDate)}.");
        }

        if (liters <= 0 || !TextInput.HasTwoDecimalsAtMost(liters))
        {
            throw ApiException.Unprocessable("Los litros usados deben ser mayores que 0, con dos decimales como máximo.");
        }

        if (liters > lot.TotalLiters)
        {
            throw ApiException.Unprocessable($"La ordeña del {Day(lot.CollectionDate)} juntó {lot.TotalLiters:0.##} L; la tanda no puede usar más.");
        }

        var expires = expiresOn ?? madeOn.AddDays(BatchShelfLifeDays);
        if (expires <= madeOn || expires > madeOn.AddDays(BatchMaxShelfLifeDays))
        {
            throw ApiException.Unprocessable($"La caducidad va del día siguiente a la elaboración hasta {BatchMaxShelfLifeDays} días después ({Day(madeOn.AddDays(BatchMaxShelfLifeDays))}).");
        }

        return (liters, expires);
    }

    private async Task<List<(int ProductId, decimal Quantity)>> ValidateOutputsAsync(IReadOnlyList<BatchOutputRequest>? outputs, CancellationToken cancellationToken)
    {
        var items = (outputs ?? []).Where(output => output.ProductId is not null || output.Quantity is not null).ToList();
        if (items.Count == 0)
        {
            throw ApiException.BadRequest("Indica lo que salió de la tanda: kg de canasta o piezas de panela.");
        }

        if (items.Any(output => output.ProductId is null))
        {
            throw ApiException.BadRequest("Cada presentación necesita su producto.");
        }

        if (items.Select(output => output.ProductId).Distinct().Count() != items.Count)
        {
            throw ApiException.BadRequest("Una presentación aparece dos veces en la tanda.");
        }

        var ids = items.Select(output => output.ProductId!.Value).ToList();
        var products = await db.Products.AsNoTracking()
            .Where(product => ids.Contains(product.Id))
            .Select(product => new { product.Id, product.Name, product.SaleUnit, product.ProductType.Kind })
            .ToListAsync(cancellationToken);
        var result = new List<(int, decimal)>();
        foreach (var output in items)
        {
            var product = products.FirstOrDefault(item => item.Id == output.ProductId)
                ?? throw ApiException.NotFound("Uno de los productos de la tanda no existe.");
            if (product.Kind != ProductType.OwnCheese)
            {
                throw ApiException.Unprocessable($"Una tanda solo produce queso propio; {product.Name} no lo es.");
            }

            result.Add((product.Id, ValidateQuantity(output.Quantity, product.SaleUnit, product.Name, $"Indica cuánto salió de {product.Name}.")));
        }

        return result;
    }

    private sealed record PurchaseFields(int SupplierId, int ProductId, DateOnly PurchaseDate, DateOnly ExpiresOn, decimal Quantity, decimal UnitCost, string? Notes);

    private async Task<PurchaseFields> ValidatePurchaseAsync(SavePurchaseRequest request, CheesePurchase? current, CancellationToken cancellationToken)
    {
        var supplierId = request.SupplierId ?? throw ApiException.BadRequest("Elige el proveedor.");
        if (!await db.Suppliers.AnyAsync(supplier => supplier.Id == supplierId, cancellationToken))
        {
            throw ApiException.NotFound("El proveedor no existe.");
        }

        var resale = await db.Products.AsNoTracking()
            .Where(product => product.ProductType.Kind == ProductType.ResaleCheese)
            .Select(product => new { product.Id, product.Name, product.SaleUnit })
            .ToListAsync(cancellationToken);
        var productId = request.ProductId ?? current?.ProductId ?? (resale.Count == 1 ? resale[0].Id : throw ApiException.BadRequest("Elige el queso comprado."));
        if (current is not null && productId != current.ProductId)
        {
            throw ApiException.Unprocessable("El queso de una compra no se cambia; elimínala y regístrala de nuevo.");
        }

        var product = resale.FirstOrDefault(item => item.Id == productId);
        if (product is null)
        {
            var name = await db.Products.Where(item => item.Id == productId).Select(item => item.Name).FirstOrDefaultAsync(cancellationToken)
                ?? throw ApiException.NotFound("El producto no existe.");
            throw ApiException.Unprocessable($"Solo se registra la compra de queso de reventa; {name} se elabora en la quesería.");
        }

        var date = request.PurchaseDate ?? throw ApiException.BadRequest("Indica la fecha de compra.");
        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de compra no puede ser futura.");
        }

        var expires = request.ExpiresOn ?? date.AddDays(PurchaseShelfLifeDays);
        if (expires <= date)
        {
            throw ApiException.Unprocessable("La caducidad debe ser posterior a la fecha de compra.");
        }

        var quantity = ValidateQuantity(request.Quantity, product.SaleUnit, product.Name, "Indica cuántas piezas se compraron.");
        var cost = request.UnitCost ?? throw ApiException.BadRequest("Indica el costo por pieza.");
        if (cost < 0 || cost > MaxUnitCost || !TextInput.HasTwoDecimalsAtMost(cost))
        {
            throw ApiException.Unprocessable("El costo por pieza va de $0 a $100,000 con dos decimales como máximo.");
        }

        return new PurchaseFields(supplierId, productId, date, expires, quantity, cost, CleanNotes(request.Notes));
    }

    private static decimal ValidateQuantity(decimal? value, string saleUnit, string productName, string missing)
    {
        var quantity = value ?? throw ApiException.BadRequest(missing);
        if (quantity <= 0 || quantity > MaxQuantity)
        {
            throw ApiException.Unprocessable($"La cantidad de {productName} debe ser mayor que 0 y hasta {MaxQuantity:#,0}.");
        }

        if (saleUnit == Product.UnitPiece && quantity != decimal.Truncate(quantity))
        {
            throw ApiException.Unprocessable($"{productName} se cuenta en piezas enteras.");
        }

        if (decimal.Round(quantity, 3) != quantity)
        {
            throw ApiException.Unprocessable($"Los kg de {productName} admiten tres decimales como máximo.");
        }

        return quantity;
    }

    private static string? CleanNotes(string? notes)
    {
        var clean = TextInput.Clean(notes);
        return clean?.Length > 255 ? throw ApiException.BadRequest("Las notas admiten hasta 255 caracteres.") : clean;
    }

    // ------------------------------------------------------------------ Consultas y proyecciones

    private IQueryable<CheeseBatch> BatchQuery() =>
        db.CheeseBatches.AsNoTracking()
            .Include(batch => batch.MilkLot)
            .Include(batch => batch.Outputs).ThenInclude(output => output.Product);

    private IQueryable<CheesePurchase> PurchaseQuery() =>
        db.CheesePurchases.AsNoTracking().Include(purchase => purchase.Supplier).Include(purchase => purchase.Product);

    private sealed record KindedProduct(string Kind, CheeseProduct Item);

    private IQueryable<KindedProduct> CheeseProducts() =>
        db.Products.AsNoTracking()
            .Where(product => product.ProductType.Kind == ProductType.OwnCheese || product.ProductType.Kind == ProductType.ResaleCheese)
            .OrderBy(product => product.Sku)
            .Select(product => new KindedProduct(
                product.ProductType.Kind,
                new CheeseProduct(product.Id, product.Sku, product.Name, product.SaleUnit, product.UnitPrice)));

    private async Task<Dictionary<int, decimal>> ConsumedAsync(string source, IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        await db.CheeseStock.AsNoTracking()
            .Where(stock => stock.Source == source && ids.Contains(stock.SourceId))
            .ToDictionaryAsync(stock => stock.SourceId, stock => stock.Adjusted + stock.Sold, cancellationToken);

    private async Task<List<CheeseBatchItem>> ToBatchItemsAsync(IReadOnlyList<CheeseBatch> batches, CancellationToken cancellationToken)
    {
        var outputIds = batches.SelectMany(batch => batch.Outputs).Select(output => output.Id).ToList();
        var consumed = await ConsumedAsync(CheeseStock.FromBatch, outputIds, cancellationToken);
        return batches.Select(batch => new CheeseBatchItem(
                batch.Id,
                batch.Code,
                batch.MilkLotId,
                batch.MilkLot.Code,
                batch.MilkLot.CollectionDate,
                batch.MilkLot.TotalLiters,
                batch.MadeOn,
                batch.ExpiresOn,
                batch.LitersUsed,
                batch.Notes,
                batch.Outputs
                    .OrderBy(output => output.Product.Sku)
                    .Select(output =>
                    {
                        var used = consumed.GetValueOrDefault(output.Id);
                        return new BatchOutput(output.Id, output.ProductId, output.Product.Sku, output.Product.Name, output.Product.SaleUnit, output.Quantity, used, output.Quantity - used);
                    })
                    .ToList()))
            .ToList();
    }

    private async Task<List<CheesePurchaseItem>> ToPurchaseItemsAsync(IReadOnlyList<CheesePurchase> purchases, CancellationToken cancellationToken)
    {
        var consumed = await ConsumedAsync(CheeseStock.FromPurchase, purchases.Select(purchase => purchase.Id).ToList(), cancellationToken);
        return purchases.Select(purchase =>
            {
                var used = consumed.GetValueOrDefault(purchase.Id);
                return new CheesePurchaseItem(
                    purchase.Id,
                    purchase.Code,
                    purchase.SupplierId,
                    purchase.Supplier.CompanyName,
                    purchase.ProductId,
                    purchase.Product.Sku,
                    purchase.Product.Name,
                    purchase.PurchaseDate,
                    purchase.ExpiresOn,
                    purchase.Quantity,
                    used,
                    purchase.Quantity - used,
                    purchase.UnitCost,
                    decimal.Round(purchase.Quantity * purchase.UnitCost, 2),
                    purchase.Notes);
            })
            .ToList();
    }

    private async Task<List<StockAdjustmentItem>> ToAdjustmentItemsAsync(IReadOnlyList<CheeseStockAdjustment> adjustments, CancellationToken cancellationToken)
    {
        var outputIds = adjustments.Where(item => item.CheeseBatchOutputId is not null).Select(item => item.CheeseBatchOutputId!.Value).ToList();
        var purchaseIds = adjustments.Where(item => item.CheesePurchaseId is not null).Select(item => item.CheesePurchaseId!.Value).ToList();

        // Incluye entradas ya eliminadas para que el historial siga mostrando de dónde salió cada merma.
        var outputs = await db.CheeseBatchOutputs.IgnoreQueryFilters().AsNoTracking()
            .Where(output => outputIds.Contains(output.Id))
            .Select(output => new { output.Id, output.CheeseBatch.Code, output.Product.Name, output.Product.SaleUnit })
            .ToDictionaryAsync(output => output.Id, cancellationToken);
        var purchases = await db.CheesePurchases.IgnoreQueryFilters().AsNoTracking()
            .Where(purchase => purchaseIds.Contains(purchase.Id))
            .Select(purchase => new { purchase.Id, purchase.Code, purchase.Product.Name, purchase.Product.SaleUnit })
            .ToDictionaryAsync(purchase => purchase.Id, cancellationToken);

        return adjustments.Select(item =>
            {
                if (item.CheeseBatchOutputId is int outputId)
                {
                    var output = outputs[outputId];
                    return new StockAdjustmentItem(item.Id, item.AdjustmentDate, item.Reason, item.Quantity, item.Notes, CheeseStock.FromBatch, outputId, output.Code, output.Name, output.SaleUnit);
                }

                var purchase = purchases[item.CheesePurchaseId!.Value];
                return new StockAdjustmentItem(item.Id, item.AdjustmentDate, item.Reason, item.Quantity, item.Notes, CheeseStock.FromPurchase, purchase.Id, purchase.Code, purchase.Name, purchase.SaleUnit);
            })
            .ToList();
    }

    private static StockItem ToStockItem(CheeseStock stock) => new(
        stock.Source,
        stock.SourceId,
        stock.ParentId,
        stock.Code,
        stock.ProductId,
        stock.Sku,
        stock.ProductName,
        stock.SaleUnit,
        stock.EntryDate,
        stock.ExpiresOn,
        stock.ExpiresOn.DayNumber - RanchClock.Today.DayNumber,
        stock.QuantityIn,
        stock.Adjusted,
        stock.Sold,
        stock.Available);

    private static (DateOnly Start, DateOnly End) Range(DateOnly? from, DateOnly? to)
    {
        var end = to ?? RanchClock.Today;
        var start = from ?? end.AddDays(-DefaultRangeDays);
        return start > end ? throw ApiException.BadRequest("La fecha inicial no puede ser posterior a la final.") : (start, end);
    }

    private async Task<string> NextCodeAsync(string prefix, DateOnly date, bool batches, CancellationToken cancellationToken)
    {
        // El código es único aun entre registros borrados; si el día ya tiene uno, lleva sufijo.
        var code = $"{prefix}-{date:yyyyMMdd}";
        var taken = batches
            ? await db.CheeseBatches.IgnoreQueryFilters().Where(item => item.Code == code || item.Code.StartsWith(code + "-")).Select(item => item.Code).ToListAsync(cancellationToken)
            : await db.CheesePurchases.IgnoreQueryFilters().Where(item => item.Code == code || item.Code.StartsWith(code + "-")).Select(item => item.Code).ToListAsync(cancellationToken);
        if (!taken.Contains(code))
        {
            return code;
        }

        var suffix = 2;
        while (taken.Contains($"{code}-{suffix}"))
        {
            suffix++;
        }

        return $"{code}-{suffix}";
    }

    private static string Day(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Quantity(decimal value, string saleUnit) =>
        saleUnit == Product.UnitKg
            ? $"{value.ToString("0.###", CultureInfo.InvariantCulture)} kg"
            : $"{value:0} {(value == 1 ? "pieza" : "piezas")}";
}
