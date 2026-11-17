using Microsoft.AspNetCore.Mvc;
using TecNM.Api.Core.Security;

namespace TecNM.Api.Modules.Reproduction;

[ApiController]
[Route("api/reproduction")]
public sealed class ReproductionController(ReproductionService reproduction) : ControllerBase
{
    [RequirePermission(Permissions.ReproRead)]
    [HttpGet("females")]
    public Task<IReadOnlyList<FemaleStatus>> ListFemales([FromQuery] short? speciesId, [FromQuery] string? status, CancellationToken cancellationToken) =>
        reproduction.ListFemalesAsync(speciesId, status, cancellationToken);

    [RequirePermission(Permissions.ReproRead)]
    [HttpGet("breedings")]
    public Task<IReadOnlyList<BreedingItem>> ListBreedings(
        [FromQuery] int? femaleId, [FromQuery] string? outcome, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        reproduction.ListBreedingsAsync(femaleId, outcome, from, to, cancellationToken);

    [RequirePermission(Permissions.ReproWrite)]
    [HttpPost("breedings")]
    public async Task<ActionResult<BreedingItem>> CreateBreeding(CreateBreedingRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await reproduction.CreateBreedingAsync(request, cancellationToken));

    [RequirePermission(Permissions.ReproDelete)]
    [HttpDelete("breedings/{id:int}")]
    public async Task<IActionResult> DeleteBreeding(int id, CancellationToken cancellationToken)
    {
        await reproduction.DeleteBreedingAsync(id, cancellationToken);
        return NoContent();
    }

    [RequirePermission(Permissions.ReproWrite)]
    [HttpPost("breedings/{id:int}/checks")]
    public async Task<ActionResult<BreedingItem>> AddCheck(int id, CreateCheckRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await reproduction.AddCheckAsync(id, request, cancellationToken));

    [RequirePermission(Permissions.ReproRead)]
    [HttpGet("births")]
    public Task<IReadOnlyList<BirthItem>> ListBirths([FromQuery] int? damId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        reproduction.ListBirthsAsync(damId, from, to, cancellationToken);

    [RequirePermission(Permissions.ReproWrite)]
    [HttpPost("births")]
    public async Task<ActionResult<BirthItem>> CreateBirth(CreateBirthRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await reproduction.CreateBirthAsync(request, cancellationToken));
}
