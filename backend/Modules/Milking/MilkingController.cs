using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Milking;

[ApiController]
[Route("api/milking")]
public sealed class MilkingController(MilkingService milking) : ControllerBase
{
    [HttpGet("goats")]
    public Task<IReadOnlyList<MilkingGoat>> ListGoats([FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        milking.ListGoatsAsync(date, cancellationToken);

    [HttpGet("days")]
    public Task<IReadOnlyList<MilkDaySummary>> ListDays([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        milking.ListDaysAsync(from, to, cancellationToken);

    [HttpGet("days/{id:int}", Name = nameof(GetDay))]
    public Task<MilkDayDetail> GetDay(int id, CancellationToken cancellationToken) =>
        milking.GetDayAsync(id, cancellationToken);

    [HttpPost("days")]
    public async Task<ActionResult<MilkDayDetail>> CreateDay(CreateMilkDayRequest request, CancellationToken cancellationToken)
    {
        var day = await milking.CreateDayAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetDay), new { id = day.Id }, day);
    }

    [HttpPut("days/{id:int}")]
    public Task<MilkDayDetail> UpdateDay(int id, UpdateMilkDayRequest request, CancellationToken cancellationToken) =>
        milking.UpdateDayAsync(id, request, cancellationToken);

    [HttpDelete("days/{id:int}")]
    public async Task<IActionResult> DeleteDay(int id, CancellationToken cancellationToken)
    {
        await milking.DeleteDayAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("summary")]
    public Task<MilkSummary> GetSummary(CancellationToken cancellationToken) =>
        milking.GetSummaryAsync(cancellationToken);
}
