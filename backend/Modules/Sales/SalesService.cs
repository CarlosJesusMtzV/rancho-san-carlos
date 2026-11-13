using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Sales;

public sealed partial class SalesService(TecNMDbContext db)
{
    private const int DefaultRangeDays = 90;
    private const int DueSoonDays = 2;
    private const decimal MaxPrice = 1_000_000m;
    private const decimal MaxLiveKg = 1_000m;
    private const decimal MaxCheeseQuantity = 10_000m;
    private const string KindCheese = "cheese";
    private const string KindAnimal = "animal";
    private const string KindCarcass = "carcass";
    private const string SkuCalf = "GAN-BEC";
    private const string SkuKid = "GAN-CAB";
    private const string SkuCarcass = "CAB-CAN";

    // ------------------------------------------------------------------ Catálogo y clientes

    public async Task<SalesCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var products = await db.Products.AsNoTracking()
            .OrderBy(product => product.Sku)
            .Select(product => new SalesProduct(product.Id, product.Sku, product.Name, KindOf(product.ProductType.Kind), product.SaleUnit, product.UnitPrice))
            .ToListAsync(cancellationToken);
        var types = await db.CustomerTypes.AsNoTracking().OrderBy(type => type.Id)
            .Select(type => new CustomerTypeItem(type.Id, type.Name)).ToListAsync(cancellationToken);
        return new SalesCatalog(products, types);
    }

    public async Task<IReadOnlyList<CustomerItem>> ListCustomersAsync(string? search, CancellationToken cancellationToken)
    {
        var query = db.Customers.AsNoTracking();
        if (TextInput.Clean(search) is { } text)
        {
            var pattern = $"%{text.ToLower()}%";
            query = query.Where(customer => EF.Functions.Like(customer.LegalName.ToLower(), pattern)
                || (customer.Rfc != null && EF.Functions.Like(customer.Rfc.ToLower(), pattern))
                || (customer.Phone != null && EF.Functions.Like(customer.Phone, pattern)));
        }

        return await query.OrderBy(customer => customer.LegalName)
            .Select(customer => new CustomerItem(customer.Id, customer.LegalName, customer.CustomerTypeId, customer.CustomerType.Name, customer.Rfc, customer.Phone, customer.Email, customer.Address))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerItem> CreateCustomerAsync(SaveCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new Customer();
        await ApplyCustomerAsync(customer, request, cancellationToken);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return await GetCustomerAsync(customer.Id, cancellationToken);
    }

    public async Task<CustomerItem> UpdateCustomerAsync(int id, SaveCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El cliente no existe.");
        await ApplyCustomerAsync(customer, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetCustomerAsync(customer.Id, cancellationToken);
    }

    // ------------------------------------------------------------------ Lo que se puede vender

    public async Task<AvailableItems> ListAvailableAsync(DateOnly? date, int? orderId, CancellationToken cancellationToken)
    {
        var day = date ?? RanchClock.Today;
        var held = orderId is { } id ? await HeldByOrderAsync(id, cancellationToken) : [];

        var stock = await db.CheeseStock.AsNoTracking().Where(item => item.ExpiresOn >= day).ToListAsync(cancellationToken);
        var prices = await db.Products.AsNoTracking().ToDictionaryAsync(product => product.Id, product => product.UnitPrice, cancellationToken);
        var cheese = stock
            .Select(item => new { item, available = item.Available + held.GetValueOrDefault((item.Source, item.SourceId)) })
            .Where(entry => entry.available > 0)
            .OrderBy(entry => entry.item.ExpiresOn).ThenBy(entry => entry.item.Code)
            .Select(entry => new AvailableCheese(entry.item.Source, entry.item.SourceId, entry.item.Code, entry.item.ProductId, entry.item.ProductName,
                entry.item.SaleUnit, entry.available, entry.item.ExpiresOn, prices.GetValueOrDefault(entry.item.ProductId)))
            .ToList();

        var products = await db.Products.AsNoTracking().Where(product => product.Sku == SkuCalf || product.Sku == SkuKid || product.Sku == SkuCarcass)
            .ToDictionaryAsync(product => product.Sku, cancellationToken);
        var reservedAnimals = await ReservedAsync(item => item.AnimalId, orderId, cancellationToken);
        var animals = await db.Animals.AsNoTracking()
            .Where(animal => animal.LifecycleStatus == AnimalLifecycle.Active && !reservedAnimals.Contains(animal.Id))
            .Select(animal => new
            {
                animal.Id,
                animal.VisualTag,
                animal.SpeciesId,
                animal.Sex,
                animal.BirthDate,
                LastWeight = animal.Weights.OrderByDescending(weight => weight.WeighedOn).Select(weight => (decimal?)weight.WeightKg).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        var withdrawals = await MeatWithdrawalsAsync(animals.Select(animal => animal.Id).ToList(), day, cancellationToken);
        var animalItems = animals
            .OrderBy(animal => animal.SpeciesId).ThenBy(animal => animal.BirthDate is null).ThenByDescending(animal => animal.BirthDate).ThenBy(animal => animal.VisualTag)
            .Select(animal =>
            {
                var product = products[animal.SpeciesId == Species.CattleId ? SkuCalf : SkuKid];
                DateOnly? allowed = withdrawals.TryGetValue(animal.Id, out var until) ? until : null;
                return new AvailableAnimal(animal.Id, animal.VisualTag, product.Id, product.Name, animal.Sex, AnimalAge.InMonths(animal.BirthDate, day), animal.LastWeight, allowed);
            })
            .ToList();

        var reservedCarcasses = await ReservedAsync(item => item.KidSlaughterId, orderId, cancellationToken);
        var carcassProductId = products[SkuCarcass].Id;
        var carcasses = await db.KidSlaughters.AsNoTracking()
            .Where(kid => !reservedCarcasses.Contains(kid.Id))
            .OrderBy(kid => kid.SlaughterRun.RunDate).ThenBy(kid => kid.Animal.VisualTag)
            .Select(kid => new AvailableCarcass(kid.Id, kid.Animal.VisualTag, carcassProductId, kid.CarcassWeightKg, kid.SlaughterRun.RunDate))
            .ToListAsync(cancellationToken);
        return new AvailableItems(cheese, animalItems, carcasses);
    }

    // ------------------------------------------------------------------ Pedidos

    public async Task<IReadOnlyList<OrderSummary>> ListOrdersAsync(string? status, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var end = to ?? RanchClock.Today;
        var start = from ?? end.AddDays(-DefaultRangeDays);
        if (start > end)
        {
            throw ApiException.BadRequest("La fecha inicial no puede ser posterior a la final.");
        }

        var filter = status?.Trim().ToLowerInvariant();
        if (filter is not (null or "" or "all" or Order.Confirmed or Order.Delivered or Order.Cancelled))
        {
            throw ApiException.BadRequest("El estado debe ser confirmed, delivered, cancelled o all.");
        }

        var query = db.Orders.AsNoTracking().Include(order => order.Customer).Include(order => order.Items)
            .Where(order => (order.OrderDate >= start && order.OrderDate <= end) || order.OrderStatus == Order.Confirmed);
        if (filter is Order.Confirmed or Order.Delivered or Order.Cancelled)
        {
            query = query.Where(order => order.OrderStatus == filter);
        }

        var orders = await query.OrderByDescending(order => order.OrderDate).ThenByDescending(order => order.Id).ToListAsync(cancellationToken);
        var ids = orders.Select(order => order.Id).ToList();
        var folios = await db.Invoices.AsNoTracking().Where(invoice => ids.Contains(invoice.OrderId))
            .ToDictionaryAsync(invoice => invoice.OrderId, invoice => invoice.FiscalFolio, cancellationToken);
        return orders.Select(order => ToSummary(order, folios.GetValueOrDefault(order.Id))).ToList();
    }

    public async Task<OrderDetail> GetOrderAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.Items).ThenInclude(line => line.Product).ThenInclude(product => product.ProductType)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El pedido no existe.");
        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(item => item.OrderId == id, cancellationToken);
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(item => item.OrderId == id, cancellationToken);
        var lines = await ToLinesAsync(order.Items, cancellationToken);
        var summary = ToSummary(order, invoice?.FiscalFolio);
        return new OrderDetail(summary.Id, summary.CustomerId, summary.CustomerName, summary.OrderDate, summary.EstimatedDeliveryDate, summary.Status,
            summary.DeliveryType, summary.DeliveredOn, summary.ItemCount, summary.Total, summary.InvoiceFolio, order.DeliveryAddress, order.Notes, lines,
            payment is null ? null : new PaymentItem(payment.PaymentDate, payment.Amount, payment.PaymentMethod),
            invoice is null ? null : new InvoiceItem(invoice.FiscalFolio, invoice.IssueDate, invoice.Subtotal, invoice.Taxes, invoice.Total));
    }

    public async Task<OrderDetail> CreateOrderAsync(SaveOrderRequest request, CancellationToken cancellationToken)
    {
        var date = request.OrderDate ?? throw ApiException.BadRequest("Indica la fecha del pedido.");
        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha del pedido no puede ser futura.");
        }

        var paymentMethod = request.DeliverNow is null ? null : ValidatePaymentMethod(request.DeliverNow.PaymentMethod);
        var order = new Order { OrderDate = date };
        await ApplyHeaderAsync(order, request, cancellationToken);
        var lines = await ValidateLinesAsync(request.Items, date, null, cancellationToken);
        if (paymentMethod is not null && order.EstimatedDeliveryDate is not null)
        {
            throw ApiException.BadRequest("Una venta en el momento no lleva fecha estimada de entrega.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        order.OrderStatus = paymentMethod is null ? Order.Confirmed : Order.Delivered;
        order.DeliveredOn = paymentMethod is null ? null : date;
        order.Items = lines.Select(line => line.Item).ToList();
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        if (paymentMethod is not null)
        {
            await CompleteDeliveryAsync(order, date, paymentMethod, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetOrderAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDetail> UpdateOrderAsync(int id, SaveOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El pedido no existe.");
        if (order.OrderStatus != Order.Confirmed)
        {
            throw ApiException.Conflict(order.OrderStatus == Order.Delivered
                ? "Una venta entregada no se edita; si hubo un error, cancélala y regístrala de nuevo."
                : "Un pedido cancelado no se edita.");
        }

        if (request.DeliverNow is not null)
        {
            throw ApiException.BadRequest("Para entregar un encargo usa «Entregar y cobrar».");
        }

        var date = request.OrderDate ?? order.OrderDate;
        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha del pedido no puede ser futura.");
        }

        order.OrderDate = date;
        await ApplyHeaderAsync(order, request, cancellationToken);
        var lines = await ValidateLinesAsync(request.Items, date, order.Id, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var item in order.Items)
        {
            item.DeletedAt = now;
            item.Status = 0;
        }

        await db.SaveChangesAsync(cancellationToken);
        order.Items.AddRange(lines.Select(line => line.Item));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetOrderAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDetail> DeliverOrderAsync(int id, DeliverOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(item => item.Items).ThenInclude(line => line.Product)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El pedido no existe.");
        if (order.OrderStatus != Order.Confirmed)
        {
            throw ApiException.Conflict(order.OrderStatus == Order.Delivered ? "El pedido ya se entregó." : "Un pedido cancelado no se entrega.");
        }

        var method = ValidatePaymentMethod(request.PaymentMethod);
        var deliveredOn = request.DeliveredOn ?? RanchClock.Today;
        if (deliveredOn > RanchClock.Today || deliveredOn < order.OrderDate)
        {
            throw ApiException.Unprocessable($"La entrega va del {Day(order.OrderDate)} a hoy.");
        }

        // Lo apartado pudo caducar o entrar en retiro después del encargo.
        await EnsureDeliverableAsync(order, deliveredOn, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        order.OrderStatus = Order.Delivered;
        order.DeliveredOn = deliveredOn;
        await db.SaveChangesAsync(cancellationToken);
        await CompleteDeliveryAsync(order, deliveredOn, method, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetOrderAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDetail> CancelOrderAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El pedido no existe.");
        if (order.OrderStatus == Order.Cancelled)
        {
            throw ApiException.Conflict("El pedido ya está cancelado.");
        }

        if (await db.Invoices.AnyAsync(invoice => invoice.OrderId == id, cancellationToken))
        {
            throw ApiException.Conflict("La venta tiene factura; quítala primero (y cancélala ante el SAT si ya se timbró).");
        }

        var now = DateTime.UtcNow;
        if (order.OrderStatus == Order.Delivered)
        {
            var animalIds = order.Items.Where(item => item.AnimalId is not null).Select(item => item.AnimalId!.Value).ToList();
            var animals = await db.Animals.Where(animal => animalIds.Contains(animal.Id) && animal.LifecycleStatus == AnimalLifecycle.Sold).ToListAsync(cancellationToken);
            foreach (var animal in animals)
            {
                animal.LifecycleStatus = AnimalLifecycle.Active;
                animal.ExitDate = null;
            }

            var payment = await db.Payments.FirstOrDefaultAsync(item => item.OrderId == id, cancellationToken);
            if (payment is not null)
            {
                payment.DeletedAt = now;
                payment.Status = 0;
            }
        }

        order.OrderStatus = Order.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDetail> SaveInvoiceAsync(int id, SaveInvoiceRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(item => item.Customer).Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El pedido no existe.");
        if (order.OrderStatus != Order.Delivered)
        {
            throw ApiException.Conflict("Solo se factura una venta entregada.");
        }

        if (order.Customer.Rfc is null)
        {
            throw ApiException.Unprocessable($"{order.Customer.LegalName} no tiene RFC; agrégalo en Clientes para facturar.");
        }

        var folio = TextInput.CleanCode(request.FiscalFolio) ?? throw ApiException.BadRequest("Escribe el folio fiscal.");
        if (folio.Length > 40)
        {
            throw ApiException.BadRequest("El folio fiscal admite hasta 40 caracteres.");
        }

        var issueDate = request.IssueDate ?? throw ApiException.BadRequest("Indica la fecha de la factura.");
        if (issueDate > RanchClock.Today || issueDate < order.OrderDate)
        {
            throw ApiException.Unprocessable($"La fecha de la factura va del {Day(order.OrderDate)} a hoy.");
        }

        var taxes = request.Taxes ?? 0;
        if (taxes < 0 || taxes > MaxPrice || !TextInput.HasTwoDecimalsAtMost(taxes))
        {
            throw ApiException.Unprocessable("Los impuestos no pueden ser negativos y llevan dos decimales como máximo.");
        }

        var invoice = await db.Invoices.FirstOrDefaultAsync(item => item.OrderId == id, cancellationToken);
        if (await db.Invoices.AnyAsync(item => item.FiscalFolio == folio && item.OrderId != id, cancellationToken))
        {
            throw ApiException.Conflict($"El folio {folio} ya está en otra venta.");
        }

        if (invoice is null)
        {
            invoice = new Invoice { OrderId = id };
            db.Invoices.Add(invoice);
        }

        var subtotal = order.Items.Sum(LineTotal);
        invoice.FiscalFolio = folio;
        invoice.IssueDate = issueDate;
        invoice.Subtotal = subtotal;
        invoice.Taxes = taxes;
        invoice.Total = subtotal + taxes;
        await db.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(id, cancellationToken);
    }

    public async Task<OrderDetail> DeleteInvoiceAsync(int id, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(item => item.OrderId == id, cancellationToken)
            ?? throw ApiException.NotFound("La venta no tiene factura.");
        invoice.DeletedAt = DateTime.UtcNow;
        invoice.Status = 0;
        await db.SaveChangesAsync(cancellationToken);
        return await GetOrderAsync(id, cancellationToken);
    }

    public async Task<SalesSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var today = RanchClock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var delivered = await db.OrderItems.AsNoTracking()
            .Where(item => item.Order.OrderStatus == Order.Delivered && item.Order.DeliveredOn >= monthStart && item.Order.DeliveredOn <= today)
            .Select(item => new { item.Quantity, item.UnitPrice, item.Product.ProductType.Kind })
            .ToListAsync(cancellationToken);
        decimal Sum(string kind) => delivered.Where(item => KindOf(item.Kind) == kind).Sum(item => decimal.Round(item.Quantity * item.UnitPrice, 2));
        var byKind = new SalesByKind(Sum(KindCheese), Sum(KindAnimal), Sum(KindCarcass));
        var pending = await db.Orders.AsNoTracking().Where(order => order.OrderStatus == Order.Confirmed)
            .Select(order => order.EstimatedDeliveryDate).ToListAsync(cancellationToken);
        return new SalesSummary(byKind.Cheese + byKind.Animal + byKind.Carcass, byKind, pending.Count,
            pending.Count(date => date is { } due && due <= today.AddDays(DueSoonDays)));
    }

    // ------------------------------------------------------------------ Validación

    private async Task ApplyCustomerAsync(Customer customer, SaveCustomerRequest request, CancellationToken cancellationToken)
    {
        var name = TextInput.Clean(request.Name) ?? throw ApiException.BadRequest("Escribe el nombre del cliente.");
        if (name.Length > 160)
        {
            throw ApiException.BadRequest("El nombre admite hasta 160 caracteres.");
        }

        var typeId = request.CustomerTypeId ?? throw ApiException.BadRequest("Elige el tipo de cliente.");
        if (!await db.CustomerTypes.AnyAsync(type => type.Id == typeId, cancellationToken))
        {
            throw ApiException.NotFound("El tipo de cliente no existe.");
        }

        var rfc = TextInput.CleanCode(request.Rfc);
        if (rfc is not null && !RfcPattern().IsMatch(rfc))
        {
            throw ApiException.BadRequest("El RFC tiene 12 caracteres (persona moral) o 13 (persona física), por ejemplo XAXX010101000.");
        }

        if (rfc is not null && await db.Customers.AnyAsync(item => item.Rfc == rfc && item.Id != customer.Id, cancellationToken))
        {
            throw ApiException.Conflict($"Ya hay un cliente con el RFC {rfc}.");
        }

        var email = TextInput.Clean(request.Email);
        if (email is not null && (email.Length > 254 || !email.Contains('@')))
        {
            throw ApiException.BadRequest("El correo no es válido.");
        }

        var phone = TextInput.Clean(request.Phone);
        var address = TextInput.Clean(request.Address);
        if (phone?.Length > 20 || address?.Length > 255)
        {
            throw ApiException.BadRequest("El teléfono admite 20 caracteres y la dirección 255.");
        }

        customer.LegalName = name;
        customer.CustomerTypeId = typeId;
        customer.Rfc = rfc;
        customer.Email = email;
        customer.Phone = phone;
        customer.Address = address;
    }

    private async Task ApplyHeaderAsync(Order order, SaveOrderRequest request, CancellationToken cancellationToken)
    {
        var customerId = request.CustomerId ?? throw ApiException.BadRequest("Elige el cliente.");
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == customerId, cancellationToken)
            ?? throw ApiException.NotFound("El cliente no existe.");
        if (request.EstimatedDeliveryDate is { } due && due < order.OrderDate)
        {
            throw ApiException.Unprocessable("La entrega estimada no puede ser anterior al pedido.");
        }

        var deliveryType = request.DeliveryType?.Trim().ToLowerInvariant() ?? Order.Pickup;
        if (deliveryType is not (Order.Pickup or Order.HomeDelivery))
        {
            throw ApiException.BadRequest("La entrega es «pickup» (recoge el cliente) o «delivery» (a domicilio).");
        }

        var address = deliveryType == Order.HomeDelivery ? TextInput.Clean(request.DeliveryAddress) ?? customer.Address : null;
        if (deliveryType == Order.HomeDelivery && address is null)
        {
            throw ApiException.BadRequest("Escribe la dirección de entrega.");
        }

        var notes = TextInput.Clean(request.Notes);
        if (address?.Length > 255 || notes?.Length > 255)
        {
            throw ApiException.BadRequest("La dirección y las notas admiten hasta 255 caracteres.");
        }

        order.CustomerId = customerId;
        order.EstimatedDeliveryDate = request.EstimatedDeliveryDate;
        order.DeliveryType = deliveryType;
        order.DeliveryAddress = address;
        order.Notes = notes;
    }

    private sealed record ValidLine(OrderItem Item);

    private async Task<List<ValidLine>> ValidateLinesAsync(IReadOnlyList<OrderLineRequest>? requests, DateOnly date, int? orderId, CancellationToken cancellationToken)
    {
        var items = requests ?? [];
        if (items.Count == 0)
        {
            throw ApiException.BadRequest("Agrega al menos un producto al pedido.");
        }

        var held = orderId is { } id ? await HeldByOrderAsync(id, cancellationToken) : [];
        var reservedAnimals = await ReservedAsync(item => item.AnimalId, orderId, cancellationToken);
        var reservedCarcasses = await ReservedAsync(item => item.KidSlaughterId, orderId, cancellationToken);
        var products = await db.Products.AsNoTracking().ToDictionaryAsync(product => product.Sku, cancellationToken);
        var seen = new HashSet<string>();
        var result = new List<ValidLine>();

        foreach (var line in items)
        {
            var kind = line.Kind?.Trim().ToLowerInvariant();
            var sourceId = line.SourceId ?? throw ApiException.BadRequest("Cada renglón necesita qué se vende.");
            var key = $"{kind}:{line.Source}:{sourceId}";
            if (!seen.Add(key))
            {
                throw ApiException.BadRequest("Un producto aparece dos veces en el pedido; junta las cantidades en un renglón.");
            }

            switch (kind)
            {
                case KindCheese:
                {
                    var source = line.Source?.Trim().ToLowerInvariant();
                    if (source is not (CheeseStock.FromBatch or CheeseStock.FromPurchase))
                    {
                        throw ApiException.BadRequest("El queso sale de una tanda («batch») o de una compra («purchase»).");
                    }

                    var stock = await db.CheeseStock.AsNoTracking().FirstOrDefaultAsync(item => item.Source == source && item.SourceId == sourceId, cancellationToken)
                        ?? throw ApiException.NotFound("No se encontró ese queso en existencia.");
                    if (stock.ExpiresOn < date)
                    {
                        throw ApiException.Unprocessable($"{stock.ProductName} ({stock.Code}) caducó el {Day(stock.ExpiresOn)}; regístralo como merma en Quesería.");
                    }

                    var quantity = line.Quantity ?? throw ApiException.BadRequest($"Indica la cantidad de {stock.ProductName}.");
                    if (quantity <= 0 || quantity > MaxCheeseQuantity || decimal.Round(quantity, 3) != quantity)
                    {
                        throw ApiException.Unprocessable($"La cantidad de {stock.ProductName} debe ser mayor que 0, con tres decimales como máximo.");
                    }

                    if (stock.SaleUnit == Product.UnitPiece && quantity != decimal.Truncate(quantity))
                    {
                        throw ApiException.Unprocessable($"{stock.ProductName} se vende en piezas enteras.");
                    }

                    var available = stock.Available + held.GetValueOrDefault((source, sourceId));
                    if (quantity > available)
                    {
                        throw ApiException.Unprocessable($"Solo hay {Quantity(available, stock.SaleUnit)} de {stock.ProductName} ({stock.Code}).");
                    }

                    var price = ValidatePrice(line.UnitPrice ?? (await db.Products.AsNoTracking().Where(product => product.Id == stock.ProductId).Select(product => product.UnitPrice).FirstOrDefaultAsync(cancellationToken)), stock.ProductName);
                    result.Add(new ValidLine(new OrderItem
                    {
                        ProductId = stock.ProductId,
                        CheeseBatchOutputId = source == CheeseStock.FromBatch ? sourceId : null,
                        CheesePurchaseId = source == CheeseStock.FromPurchase ? sourceId : null,
                        Quantity = quantity,
                        UnitPrice = price,
                    }));
                    break;
                }

                case KindAnimal:
                {
                    var animal = await db.Animals.AsNoTracking().FirstOrDefaultAsync(item => item.Id == sourceId, cancellationToken)
                        ?? throw ApiException.NotFound("El animal no existe.");
                    if (animal.LifecycleStatus != AnimalLifecycle.Active)
                    {
                        throw ApiException.Unprocessable($"{animal.VisualTag} ya salió del hato.");
                    }

                    if (reservedAnimals.Contains(animal.Id))
                    {
                        throw ApiException.Conflict($"{animal.VisualTag} ya está apartado en otro pedido.");
                    }

                    var allowed = (await MeatWithdrawalsAsync([animal.Id], date, cancellationToken)).GetValueOrDefault(animal.Id);
                    if (allowed != default)
                    {
                        throw ApiException.Unprocessable($"{animal.VisualTag} tiene retiro de carne; puede venderse a partir del {Day(allowed)}.");
                    }

                    var kg = line.Quantity ?? throw ApiException.BadRequest($"Indica los kg en pie de {animal.VisualTag}.");
                    if (kg <= 0 || kg > MaxLiveKg || !TextInput.HasTwoDecimalsAtMost(kg))
                    {
                        throw ApiException.Unprocessable($"Los kg en pie van de 0.01 a {MaxLiveKg:#,0} con dos decimales como máximo.");
                    }

                    var product = products[animal.SpeciesId == Species.CattleId ? SkuCalf : SkuKid];
                    result.Add(new ValidLine(new OrderItem
                    {
                        ProductId = product.Id,
                        AnimalId = animal.Id,
                        Quantity = kg,
                        UnitPrice = ValidatePrice(line.UnitPrice, $"el kilo de {animal.VisualTag}"),
                    }));
                    break;
                }

                case KindCarcass:
                {
                    var kid = await db.KidSlaughters.AsNoTracking().Include(item => item.Animal)
                        .FirstOrDefaultAsync(item => item.Id == sourceId, cancellationToken)
                        ?? throw ApiException.NotFound("La canal no existe.");
                    if (reservedCarcasses.Contains(kid.Id))
                    {
                        throw ApiException.Conflict($"La canal de {kid.Animal.VisualTag} ya está en otro pedido.");
                    }

                    result.Add(new ValidLine(new OrderItem
                    {
                        ProductId = products[SkuCarcass].Id,
                        KidSlaughterId = kid.Id,
                        Quantity = kid.CarcassWeightKg,
                        UnitPrice = ValidatePrice(line.UnitPrice, $"el kilo de canal de {kid.Animal.VisualTag}"),
                    }));
                    break;
                }

                default:
                    throw ApiException.BadRequest("Cada renglón es de queso («cheese»), ganado en pie («animal») o canal («carcass»).");
            }
        }

        return result;
    }

    private static decimal ValidatePrice(decimal? value, string what)
    {
        var price = value ?? throw ApiException.BadRequest($"Indica el precio de {what}.");
        return price < 0 || price > MaxPrice || !TextInput.HasTwoDecimalsAtMost(price)
            ? throw ApiException.Unprocessable("Los precios van de $0 a $1,000,000 con dos decimales como máximo.")
            : price;
    }

    private static string ValidatePaymentMethod(string? method)
    {
        var value = method?.Trim().ToLowerInvariant();
        return value is not null && Payment.Methods.Contains(value)
            ? value
            : throw ApiException.BadRequest("La forma de pago es efectivo, transferencia, tarjeta o cheque.");
    }

    private async Task EnsureDeliverableAsync(Order order, DateOnly deliveredOn, CancellationToken cancellationToken)
    {
        var outputIds = order.Items.Where(item => item.CheeseBatchOutputId is not null).Select(item => item.CheeseBatchOutputId!.Value).ToList();
        var purchaseIds = order.Items.Where(item => item.CheesePurchaseId is not null).Select(item => item.CheesePurchaseId!.Value).ToList();
        var expired = await db.CheeseStock.AsNoTracking()
            .Where(stock => stock.ExpiresOn < deliveredOn
                && ((stock.Source == CheeseStock.FromBatch && outputIds.Contains(stock.SourceId)) || (stock.Source == CheeseStock.FromPurchase && purchaseIds.Contains(stock.SourceId))))
            .Select(stock => new { stock.ProductName, stock.Code, stock.ExpiresOn })
            .FirstOrDefaultAsync(cancellationToken);
        if (expired is not null)
        {
            throw ApiException.Unprocessable($"{expired.ProductName} ({expired.Code}) caducó el {Day(expired.ExpiresOn)}; cambia el queso del encargo antes de entregarlo.");
        }

        var animalIds = order.Items.Where(item => item.AnimalId is not null).Select(item => item.AnimalId!.Value).ToList();
        var withdrawals = await MeatWithdrawalsAsync(animalIds, deliveredOn, cancellationToken);
        if (withdrawals.Count > 0)
        {
            var tags = await db.Animals.Where(animal => withdrawals.Keys.Contains(animal.Id)).Select(animal => animal.VisualTag).ToListAsync(cancellationToken);
            throw ApiException.Unprocessable($"Con retiro de carne a la fecha de entrega: {string.Join(", ", tags)}.");
        }

        if (await db.Animals.AnyAsync(animal => animalIds.Contains(animal.Id) && animal.LifecycleStatus != AnimalLifecycle.Active, cancellationToken))
        {
            throw ApiException.Unprocessable("Un animal del encargo ya salió del hato; quítalo del pedido.");
        }
    }

    /// <summary>Registra el pago completo y saca del hato los animales vendidos en pie.</summary>
    private async Task CompleteDeliveryAsync(Order order, DateOnly date, string method, CancellationToken cancellationToken)
    {
        db.Payments.Add(new Payment { OrderId = order.Id, PaymentDate = date, Amount = order.Items.Sum(LineTotal), PaymentMethod = method });
        var animalIds = order.Items.Where(item => item.AnimalId is not null).Select(item => item.AnimalId!.Value).ToList();
        var animals = await db.Animals.Where(animal => animalIds.Contains(animal.Id)).ToListAsync(cancellationToken);
        foreach (var animal in animals)
        {
            animal.LifecycleStatus = AnimalLifecycle.Sold;
            animal.ExitDate = date;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ Consultas

    private async Task<CustomerItem> GetCustomerAsync(int id, CancellationToken cancellationToken) =>
        await db.Customers.AsNoTracking().Where(customer => customer.Id == id)
            .Select(customer => new CustomerItem(customer.Id, customer.LegalName, customer.CustomerTypeId, customer.CustomerType.Name, customer.Rfc, customer.Phone, customer.Email, customer.Address))
            .FirstAsync(cancellationToken);

    /// <summary>Queso que aparta un encargo, por entrada; al editarlo vuelve a estar disponible para ese mismo pedido.</summary>
    private async Task<Dictionary<(string, int), decimal>> HeldByOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var items = await db.OrderItems.AsNoTracking()
            .Where(item => item.OrderId == orderId && item.Order.OrderStatus == Order.Confirmed && (item.CheeseBatchOutputId != null || item.CheesePurchaseId != null))
            .Select(item => new { item.CheeseBatchOutputId, item.CheesePurchaseId, item.Quantity })
            .ToListAsync(cancellationToken);
        return items
            .GroupBy(item => item.CheeseBatchOutputId is { } output ? (CheeseStock.FromBatch, output) : (CheeseStock.FromPurchase, item.CheesePurchaseId!.Value))
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
    }

    /// <summary>Animales o canales en un pedido vigente (no cancelado), sin contar el pedido indicado.</summary>
    private async Task<HashSet<int>> ReservedAsync(System.Linq.Expressions.Expression<Func<OrderItem, int?>> selector, int? exceptOrderId, CancellationToken cancellationToken)
    {
        var ids = await db.OrderItems.AsNoTracking()
            .Where(item => item.OrderId != exceptOrderId && item.Order.OrderStatus != Order.Cancelled)
            .Select(selector)
            .Where(id => id != null)
            .ToListAsync(cancellationToken);
        return ids.Select(id => id!.Value).ToHashSet();
    }

    private async Task<Dictionary<int, DateOnly>> MeatWithdrawalsAsync(IReadOnlyCollection<int> animalIds, DateOnly date, CancellationToken cancellationToken)
    {
        if (animalIds.Count == 0)
        {
            return [];
        }

        var treatments = await db.Treatments.AsNoTracking()
            .Where(treatment => treatment.MeatWithdrawalDays > 0 && animalIds.Contains(treatment.HealthEvent.AnimalId) && treatment.HealthEvent.EventDate <= date)
            .Select(treatment => new { treatment.HealthEvent.AnimalId, treatment.HealthEvent.EventDate, treatment.MeatWithdrawalDays })
            .ToListAsync(cancellationToken);
        return treatments
            .Select(treatment => new { treatment.AnimalId, AllowedFrom = treatment.EventDate.AddDays(treatment.MeatWithdrawalDays) })
            .Where(item => item.AllowedFrom > date)
            .GroupBy(item => item.AnimalId)
            .ToDictionary(group => group.Key, group => group.Max(item => item.AllowedFrom));
    }

    private async Task<List<OrderLine>> ToLinesAsync(IReadOnlyList<OrderItem> items, CancellationToken cancellationToken)
    {
        var outputIds = items.Where(item => item.CheeseBatchOutputId is not null).Select(item => item.CheeseBatchOutputId!.Value).ToList();
        var purchaseIds = items.Where(item => item.CheesePurchaseId is not null).Select(item => item.CheesePurchaseId!.Value).ToList();
        var animalIds = items.Where(item => item.AnimalId is not null).Select(item => item.AnimalId!.Value).ToList();
        var kidIds = items.Where(item => item.KidSlaughterId is not null).Select(item => item.KidSlaughterId!.Value).ToList();
        var batchCodes = await db.CheeseBatchOutputs.IgnoreQueryFilters().AsNoTracking().Where(output => outputIds.Contains(output.Id))
            .Select(output => new { output.Id, output.CheeseBatch.Code })
            .ToDictionaryAsync(output => output.Id, output => output.Code, cancellationToken);
        var purchaseCodes = await db.CheesePurchases.IgnoreQueryFilters().AsNoTracking().Where(purchase => purchaseIds.Contains(purchase.Id))
            .Select(purchase => new { purchase.Id, purchase.Code })
            .ToDictionaryAsync(purchase => purchase.Id, purchase => purchase.Code, cancellationToken);
        var tags = await db.Animals.AsNoTracking().Where(animal => animalIds.Contains(animal.Id))
            .ToDictionaryAsync(animal => animal.Id, animal => animal.VisualTag, cancellationToken);
        var carcasses = await db.KidSlaughters.IgnoreQueryFilters().AsNoTracking().Where(kid => kidIds.Contains(kid.Id))
            .Select(kid => new { kid.Id, kid.Animal.VisualTag, kid.SlaughterRun.RunDate })
            .ToDictionaryAsync(kid => kid.Id, cancellationToken);

        return items.OrderBy(item => item.Id).Select(item =>
            {
                var total = LineTotal(item);
                if (item.CheeseBatchOutputId is int output)
                {
                    return new OrderLine(item.Id, KindCheese, item.ProductId, item.Product.Name, item.Product.SaleUnit, CheeseStock.FromBatch, output, $"{item.Product.Name} · tanda {batchCodes[output]}", item.Quantity, item.UnitPrice, total);
                }

                if (item.CheesePurchaseId is int purchase)
                {
                    return new OrderLine(item.Id, KindCheese, item.ProductId, item.Product.Name, item.Product.SaleUnit, CheeseStock.FromPurchase, purchase, $"{item.Product.Name} · compra {purchaseCodes[purchase]}", item.Quantity, item.UnitPrice, total);
                }

                if (item.AnimalId is int animal)
                {
                    return new OrderLine(item.Id, KindAnimal, item.ProductId, item.Product.Name, item.Product.SaleUnit, null, animal, $"{item.Product.Name} · {tags[animal]}", item.Quantity, item.UnitPrice, total);
                }

                var kid = carcasses[item.KidSlaughterId!.Value];
                return new OrderLine(item.Id, KindCarcass, item.ProductId, item.Product.Name, item.Product.SaleUnit, null, kid.Id, $"Canal de {kid.VisualTag} · matanza del {Day(kid.RunDate)}", item.Quantity, item.UnitPrice, total);
            })
            .ToList();
    }

    private static OrderSummary ToSummary(Order order, string? folio) => new(
        order.Id,
        order.CustomerId,
        order.Customer.LegalName,
        order.OrderDate,
        order.EstimatedDeliveryDate,
        order.OrderStatus,
        order.DeliveryType,
        order.DeliveredOn,
        order.Items.Count,
        order.Items.Sum(LineTotal),
        folio);

    private static decimal LineTotal(OrderItem item) => decimal.Round(item.Quantity * item.UnitPrice, 2);

    private static string KindOf(string productKind) => productKind switch
    {
        ProductType.OwnCheese or ProductType.ResaleCheese => KindCheese,
        "livestock" => KindAnimal,
        "carcass" => KindCarcass,
        _ => productKind,
    };

    private static string Day(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Quantity(decimal value, string saleUnit) =>
        saleUnit == Product.UnitPiece ? $"{value:0} {(value == 1 ? "pieza" : "piezas")}" : $"{value.ToString("0.###", CultureInfo.InvariantCulture)} kg";

    [GeneratedRegex("^[A-ZÑ&]{3,4}[0-9]{6}[A-Z0-9]{3}$")]
    private static partial Regex RfcPattern();
}
