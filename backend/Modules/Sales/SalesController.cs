using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Sales;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(SalesService sales) : ControllerBase
{
    [HttpGet("catalog")]
    public Task<SalesCatalog> GetCatalog(CancellationToken cancellationToken) => sales.GetCatalogAsync(cancellationToken);

    [HttpGet("customers")]
    public Task<IReadOnlyList<CustomerItem>> ListCustomers([FromQuery] string? search, CancellationToken cancellationToken) =>
        sales.ListCustomersAsync(search, cancellationToken);

    [HttpPost("customers")]
    public async Task<ActionResult<CustomerItem>> CreateCustomer(SaveCustomerRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await sales.CreateCustomerAsync(request, cancellationToken));

    [HttpPut("customers/{id:int}")]
    public Task<CustomerItem> UpdateCustomer(int id, SaveCustomerRequest request, CancellationToken cancellationToken) =>
        sales.UpdateCustomerAsync(id, request, cancellationToken);

    [HttpGet("available")]
    public Task<AvailableItems> ListAvailable([FromQuery] DateOnly? date, [FromQuery] int? orderId, CancellationToken cancellationToken) =>
        sales.ListAvailableAsync(date, orderId, cancellationToken);

    [HttpGet("orders")]
    public Task<IReadOnlyList<OrderSummary>> ListOrders([FromQuery] string? status, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        sales.ListOrdersAsync(status, from, to, cancellationToken);

    [HttpGet("orders/{id:int}", Name = nameof(GetOrder))]
    public Task<OrderDetail> GetOrder(int id, CancellationToken cancellationToken) => sales.GetOrderAsync(id, cancellationToken);

    [HttpPost("orders")]
    public async Task<ActionResult<OrderDetail>> CreateOrder(SaveOrderRequest request, CancellationToken cancellationToken)
    {
        var order = await sales.CreateOrderAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetOrder), new { id = order.Id }, order);
    }

    [HttpPut("orders/{id:int}")]
    public Task<OrderDetail> UpdateOrder(int id, SaveOrderRequest request, CancellationToken cancellationToken) =>
        sales.UpdateOrderAsync(id, request, cancellationToken);

    [HttpPost("orders/{id:int}/deliver")]
    public Task<OrderDetail> DeliverOrder(int id, DeliverOrderRequest request, CancellationToken cancellationToken) =>
        sales.DeliverOrderAsync(id, request, cancellationToken);

    [HttpPost("orders/{id:int}/cancel")]
    public Task<OrderDetail> CancelOrder(int id, CancellationToken cancellationToken) => sales.CancelOrderAsync(id, cancellationToken);

    [HttpPut("orders/{id:int}/invoice")]
    public Task<OrderDetail> SaveInvoice(int id, SaveInvoiceRequest request, CancellationToken cancellationToken) =>
        sales.SaveInvoiceAsync(id, request, cancellationToken);

    [HttpDelete("orders/{id:int}/invoice")]
    public Task<OrderDetail> DeleteInvoice(int id, CancellationToken cancellationToken) => sales.DeleteInvoiceAsync(id, cancellationToken);

    [HttpGet("summary")]
    public Task<SalesSummary> GetSummary(CancellationToken cancellationToken) => sales.GetSummaryAsync(cancellationToken);
}
