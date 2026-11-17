using Microsoft.AspNetCore.Mvc;
using TecNM.Api.Core.Security;

namespace TecNM.Api.Modules.Sales;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(SalesService sales) : ControllerBase
{
    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpGet("catalog")]
    public Task<SalesCatalog> GetCatalog(CancellationToken cancellationToken) => sales.GetCatalogAsync(cancellationToken);

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpGet("customers")]
    public Task<IReadOnlyList<CustomerItem>> ListCustomers([FromQuery] string? search, CancellationToken cancellationToken) =>
        sales.ListCustomersAsync(search, cancellationToken);

    [RequirePermission(Permissions.SalesCustomers)]
    [HttpPost("customers")]
    public async Task<ActionResult<CustomerItem>> CreateCustomer(SaveCustomerRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await sales.CreateCustomerAsync(request, cancellationToken));

    [RequirePermission(Permissions.SalesCustomers)]
    [HttpPut("customers/{id:int}")]
    public Task<CustomerItem> UpdateCustomer(int id, SaveCustomerRequest request, CancellationToken cancellationToken) =>
        sales.UpdateCustomerAsync(id, request, cancellationToken);

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpGet("available")]
    public Task<AvailableItems> ListAvailable([FromQuery] DateOnly? date, [FromQuery] int? orderId, CancellationToken cancellationToken) =>
        sales.ListAvailableAsync(date, orderId, cancellationToken);

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpGet("orders")]
    public Task<IReadOnlyList<OrderSummary>> ListOrders([FromQuery] string? status, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        sales.ListOrdersAsync(status, from, to, cancellationToken);

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpGet("orders/{id:int}", Name = nameof(GetOrder))]
    public Task<OrderDetail> GetOrder(int id, CancellationToken cancellationToken) => sales.GetOrderAsync(id, cancellationToken);

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpPost("orders")]
    public async Task<ActionResult<OrderDetail>> CreateOrder(SaveOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await sales.CreateOrderAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetOrder), new { id = order.Id }, order);
    }

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpPut("orders/{id:int}")]
    public Task<OrderDetail> UpdateOrder(int id, SaveOrderRequest request, CancellationToken cancellationToken) =>
        sales.UpdateOrderAsync(id, request, cancellationToken);

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpPost("orders/{id:int}/deliver")]
    public Task<OrderDetail> DeliverOrder(int id, DeliverOrderRequest request, CancellationToken cancellationToken) =>
        sales.DeliverOrderAsync(id, request, cancellationToken);

    [RequirePermission(Permissions.SalesCheese, Permissions.SalesLivestock)]
    [HttpPost("orders/{id:int}/cancel")]
    public Task<OrderDetail> CancelOrder(int id, CancellationToken cancellationToken) => sales.CancelOrderAsync(id, cancellationToken);

    [RequirePermission(Permissions.PaymentsView)]
    [HttpPut("orders/{id:int}/invoice")]
    public Task<OrderDetail> SaveInvoice(int id, SaveInvoiceRequest request, CancellationToken cancellationToken) =>
        sales.SaveInvoiceAsync(id, request, cancellationToken);

    [RequirePermission(Permissions.PaymentsView)]
    [HttpDelete("orders/{id:int}/invoice")]
    public Task<OrderDetail> DeleteInvoice(int id, CancellationToken cancellationToken) => sales.DeleteInvoiceAsync(id, cancellationToken);

    [RequirePermission(Permissions.PaymentsView)]
    [HttpGet("summary")]
    public Task<SalesSummary> GetSummary(CancellationToken cancellationToken) => sales.GetSummaryAsync(cancellationToken);
}
