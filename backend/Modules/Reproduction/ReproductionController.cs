using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Reproduction;

[ApiController]
[Route("api/reproduction")]
public sealed class ReproductionController(ReproductionService reproduction) : ControllerBase
{
    [HttpGet("females")]
    public Task<IReadOnlyList<FemaleStatus>> ListFemales([FromQuery] short? speciesId, [FromQuery] string? status, CancellationToken cancellationToken) =>
        reproduction.ListFemalesAsync(speciesId, status, cancellationToken);

    [HttpGet("breedings")]
    public Task<IReadOnlyList<BreedingItem>> ListBreedings(
        [FromQuery] int? femaleId, [FromQuery] string? outcome, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        reproduction.ListBreedingsAsync(femaleId, outcome, from, to, cancellationToken);

    [HttpPost("breedings")]
    public async Task<ActionResult<BreedingItem>> CreateBreeding(CreateBreedingRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await reproduction.CreateBreedingAsync(request, cancellationToken));

    [HttpDelete("breedings/{id:int}")]
    public async Task<IActionResult> DeleteBreeding(int id, CancellationToken cancellationToken)
    {
        await reproduction.DeleteBreedingAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("breedings/{id:int}/checks")]
    public async Task<ActionResult<BreedingItem>> AddCheck(int id, CreateCheckRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await reproduction.AddCheckAsync(id, request, cancellationToken));

    [HttpGet("births")]
    public Task<IReadOnlyList<BirthItem>> ListBirths([FromQuery] int? damId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        reproduction.ListBirthsAsync(damId, from, to, cancellationToken);

    [HttpPost("births")]
    public async Task<ActionResult<BirthItem>> CreateBirth(CreateBirthRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await reproduction.CreateBirthAsync(request, cancellationToken));
}
