using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Cheese;

[ApiController]
[Route("api/cheese")]
public sealed class CheeseController(CheeseService cheese) : ControllerBase
{
    [HttpGet("catalog")]
    public Task<CheeseCatalog> GetCatalog(CancellationToken cancellationToken) => cheese.GetCatalogAsync(cancellationToken);

    [HttpPost("suppliers")]
    public async Task<ActionResult<CheeseSupplier>> CreateSupplier(CreateSupplierRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await cheese.CreateSupplierAsync(request, cancellationToken));

    [HttpGet("milk-lots")]
    public Task<IReadOnlyList<AvailableMilkLot>> ListMilkLots(CancellationToken cancellationToken) =>
        cheese.ListAvailableMilkLotsAsync(cancellationToken);

    [HttpGet("batches")]
    public Task<IReadOnlyList<CheeseBatchItem>> ListBatches([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        cheese.ListBatchesAsync(from, to, cancellationToken);

    [HttpGet("batches/{id:int}", Name = nameof(GetBatch))]
    public Task<CheeseBatchItem> GetBatch(int id, CancellationToken cancellationToken) => cheese.GetBatchAsync(id, cancellationToken);

    [HttpPost("batches")]
    public async Task<ActionResult<CheeseBatchItem>> CreateBatch(CreateBatchRequest request, CancellationToken cancellationToken)
    {
        var batch = await cheese.CreateBatchAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetBatch), new { id = batch.Id }, batch);
    }

    [HttpPut("batches/{id:int}")]
    public Task<CheeseBatchItem> UpdateBatch(int id, UpdateBatchRequest request, CancellationToken cancellationToken) =>
        cheese.UpdateBatchAsync(id, request, cancellationToken);

    [HttpDelete("batches/{id:int}")]
    public async Task<IActionResult> DeleteBatch(int id, CancellationToken cancellationToken)
    {
        await cheese.DeleteBatchAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("purchases")]
    public Task<IReadOnlyList<CheesePurchaseItem>> ListPurchases([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        cheese.ListPurchasesAsync(from, to, cancellationToken);

    [HttpGet("purchases/{id:int}", Name = nameof(GetPurchase))]
    public Task<CheesePurchaseItem> GetPurchase(int id, CancellationToken cancellationToken) => cheese.GetPurchaseAsync(id, cancellationToken);

    [HttpPost("purchases")]
    public async Task<ActionResult<CheesePurchaseItem>> CreatePurchase(SavePurchaseRequest request, CancellationToken cancellationToken)
    {
        var purchase = await cheese.CreatePurchaseAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetPurchase), new { id = purchase.Id }, purchase);
    }

    [HttpPut("purchases/{id:int}")]
    public Task<CheesePurchaseItem> UpdatePurchase(int id, SavePurchaseRequest request, CancellationToken cancellationToken) =>
        cheese.UpdatePurchaseAsync(id, request, cancellationToken);

    [HttpDelete("purchases/{id:int}")]
    public async Task<IActionResult> DeletePurchase(int id, CancellationToken cancellationToken)
    {
        await cheese.DeletePurchaseAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("stock")]
    public Task<IReadOnlyList<StockItem>> ListStock([FromQuery] bool all, CancellationToken cancellationToken) =>
        cheese.ListStockAsync(all, cancellationToken);

    [HttpGet("summary")]
    public Task<CheeseSummary> GetSummary(CancellationToken cancellationToken) => cheese.GetSummaryAsync(cancellationToken);

    [HttpGet("adjustments")]
    public Task<IReadOnlyList<StockAdjustmentItem>> ListAdjustments([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        cheese.ListAdjustmentsAsync(from, to, cancellationToken);

    [HttpPost("adjustments")]
    public async Task<ActionResult<StockAdjustmentItem>> CreateAdjustment(CreateAdjustmentRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await cheese.CreateAdjustmentAsync(request, cancellationToken));

    [HttpDelete("adjustments/{id:int}")]
    public async Task<IActionResult> DeleteAdjustment(int id, CancellationToken cancellationToken)
    {
        await cheese.DeleteAdjustmentAsync(id, cancellationToken);
        return NoContent();
    }
}
