using Microsoft.AspNetCore.Mvc;
using TecNM.Api.Core.Security;

namespace TecNM.Api.Modules.Milking;

[ApiController]
[Route("api/milking")]
public sealed class MilkingController(MilkingService milking) : ControllerBase
{
    [RequirePermission(Permissions.MilkingRead)]
    [HttpGet("goats")]
    public Task<IReadOnlyList<MilkingGoat>> ListGoats([FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        milking.ListGoatsAsync(date, cancellationToken);

    [RequirePermission(Permissions.MilkingRead)]
    [HttpGet("days")]
    public Task<IReadOnlyList<MilkDaySummary>> ListDays([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        milking.ListDaysAsync(from, to, cancellationToken);

    [RequirePermission(Permissions.MilkingRead)]
    [HttpGet("days/{id:int}", Name = nameof(GetDay))]
    public Task<MilkDayDetail> GetDay(int id, CancellationToken cancellationToken) =>
        milking.GetDayAsync(id, cancellationToken);

    [RequirePermission(Permissions.MilkingWrite)]
    [HttpPost("days")]
    public async Task<ActionResult<MilkDayDetail>> CreateDay(CreateMilkDayRequest request, CancellationToken cancellationToken)
    {
        var day = await milking.CreateDayAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetDay), new { id = day.Id }, day);
    }

    [RequirePermission(Permissions.MilkingWrite)]
    [HttpPut("days/{id:int}")]
    public Task<MilkDayDetail> UpdateDay(int id, UpdateMilkDayRequest request, CancellationToken cancellationToken) =>
        milking.UpdateDayAsync(id, request, cancellationToken);

    [RequirePermission(Permissions.MilkingDelete)]
    [HttpDelete("days/{id:int}")]
    public async Task<IActionResult> DeleteDay(int id, CancellationToken cancellationToken)
    {
        await milking.DeleteDayAsync(id, cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.MilkingRead)]
    [HttpGet("summary")]
    public Task<MilkSummary> GetSummary(CancellationToken cancellationToken) =>
        milking.GetSummaryAsync(cancellationToken);
}
