using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Slaughter;

[ApiController]
[Route("api/slaughter")]
public sealed class SlaughterController(SlaughterService slaughter) : ControllerBase
{
    [HttpGet("kids")]
    public Task<IReadOnlyList<SlaughterCandidate>> ListKids([FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        slaughter.ListKidsAsync(date, cancellationToken);

    [HttpGet("runs")]
    public Task<IReadOnlyList<SlaughterRunSummary>> ListRuns([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        slaughter.ListRunsAsync(from, to, cancellationToken);

    [HttpGet("runs/{id:int}", Name = nameof(GetRun))]
    public Task<SlaughterRunDetail> GetRun(int id, CancellationToken cancellationToken) => slaughter.GetRunAsync(id, cancellationToken);

    [HttpPost("runs")]
    public async Task<ActionResult<SlaughterRunDetail>> CreateRun(SaveRunRequest request, CancellationToken cancellationToken)
    {
        var run = await slaughter.CreateRunAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetRun), new { id = run.Id }, run);
    }

    [HttpPut("runs/{id:int}")]
    public Task<SlaughterRunDetail> UpdateRun(int id, SaveRunRequest request, CancellationToken cancellationToken) =>
        slaughter.UpdateRunAsync(id, request, cancellationToken);

    [HttpDelete("runs/{id:int}")]
    public async Task<IActionResult> DeleteRun(int id, CancellationToken cancellationToken)
    {
        await slaughter.DeleteRunAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("carcasses")]
    public Task<IReadOnlyList<Carcass>> ListCarcasses(CancellationToken cancellationToken) => slaughter.ListCarcassesAsync(cancellationToken);

    [HttpGet("summary")]
    public Task<SlaughterSummary> GetSummary(CancellationToken cancellationToken) => slaughter.GetSummaryAsync(cancellationToken);
}
