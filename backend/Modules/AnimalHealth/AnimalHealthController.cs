using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.AnimalHealth;

[ApiController]
[Route("api/animal-health")]
public sealed class AnimalHealthController(AnimalHealthService health) : ControllerBase
{
    [HttpGet("catalog")]
    public Task<HealthCatalog> GetCatalog(CancellationToken cancellationToken) =>
        health.GetCatalogAsync(cancellationToken);

    [HttpPost("medications")]
    public async Task<ActionResult<MedicationItem>> CreateMedication(MedicationInput input, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await health.CreateMedicationAsync(input, cancellationToken));

    [HttpPut("medications/{id:int}")]
    public Task<MedicationItem> UpdateMedication(int id, MedicationInput input, CancellationToken cancellationToken) =>
        health.UpdateMedicationAsync(id, input, cancellationToken);

    [HttpGet("visits")]
    public Task<IReadOnlyList<VetVisitSummary>> ListVisits([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        health.ListVisitsAsync(from, to, cancellationToken);

    [HttpGet("visits/{id:int}", Name = nameof(GetVisit))]
    public Task<VetVisitDetail> GetVisit(int id, CancellationToken cancellationToken) =>
        health.GetVisitAsync(id, cancellationToken);

    [HttpPost("visits")]
    public async Task<ActionResult<VetVisitDetail>> CreateVisit(VetVisitInput input, CancellationToken cancellationToken)
    {
        var visit = await health.CreateVisitAsync(input, cancellationToken);
        return CreatedAtRoute(nameof(GetVisit), new { id = visit.Id }, visit);
    }

    [HttpPut("visits/{id:int}")]
    public Task<VetVisitDetail> UpdateVisit(int id, VetVisitInput input, CancellationToken cancellationToken) =>
        health.UpdateVisitAsync(id, input, cancellationToken);

    [HttpGet("events")]
    public Task<HealthEventPage> ListEvents([FromQuery] HealthEventQuery query, CancellationToken cancellationToken) =>
        health.ListEventsAsync(query, cancellationToken);

    [HttpPost("events")]
    public async Task<ActionResult<CreatedEvents>> CreateEvents(CreateEventsRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await health.CreateEventsAsync(request, cancellationToken));

    [HttpDelete("events/{id:int}")]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken cancellationToken)
    {
        await health.DeleteEventAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("withdrawals")]
    public Task<IReadOnlyList<ActiveWithdrawalItem>> ListWithdrawals(CancellationToken cancellationToken) =>
        health.ListWithdrawalsAsync(cancellationToken);

    [HttpGet("lab-tests")]
    public Task<IReadOnlyList<LabTestItem>> ListLabTests([FromQuery] int? animalId, [FromQuery] string? result, CancellationToken cancellationToken) =>
        health.ListLabTestsAsync(animalId, result, cancellationToken);

    [HttpPost("lab-tests")]
    public async Task<ActionResult<LabTestItem>> CreateLabTest(CreateLabTestRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await health.CreateLabTestAsync(request, cancellationToken));

    [HttpPut("lab-tests/{id:int}/result")]
    public Task<LabTestItem> SetLabResult(int id, LabTestResultRequest request, CancellationToken cancellationToken) =>
        health.SetLabResultAsync(id, request, cancellationToken);
}
