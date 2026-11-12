using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Herd;

[ApiController]
[Route("api/herd")]
public sealed class HerdController(HerdService herd) : ControllerBase
{
    [HttpGet("catalog")]
    public Task<HerdCatalog> GetCatalog(CancellationToken cancellationToken) =>
        herd.GetCatalogAsync(cancellationToken);

    [HttpPost("breeds")]
    public async Task<ActionResult<BreedItem>> CreateBreed(CreateBreedRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await herd.CreateBreedAsync(request, cancellationToken));

    [HttpPost("lots")]
    public async Task<ActionResult<LotItem>> CreateLot(CreateLotRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await herd.CreateLotAsync(request, cancellationToken));

    [HttpGet("animals")]
    public Task<AnimalPage> ListAnimals([FromQuery] AnimalQuery query, CancellationToken cancellationToken) =>
        herd.ListAnimalsAsync(query, cancellationToken);

    [HttpGet("animals/{id:int}", Name = nameof(GetAnimal))]
    public Task<AnimalDetail> GetAnimal(int id, CancellationToken cancellationToken) =>
        herd.GetAnimalAsync(id, cancellationToken);

    [HttpPost("animals")]
    public async Task<ActionResult<AnimalDetail>> CreateAnimal(CreateAnimalRequest request, CancellationToken cancellationToken)
    {
        var animal = await herd.CreateAnimalAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(GetAnimal), new { id = animal.Id }, animal);
    }

    [HttpPut("animals/{id:int}")]
    public Task<AnimalDetail> UpdateAnimal(int id, UpdateAnimalRequest request, CancellationToken cancellationToken) =>
        herd.UpdateAnimalAsync(id, request, cancellationToken);

    [HttpPost("animals/{id:int}/exit")]
    public Task<AnimalDetail> RegisterExit(int id, AnimalExitRequest request, CancellationToken cancellationToken) =>
        herd.RegisterExitAsync(id, request, cancellationToken);

    [HttpPost("animals/{id:int}/weights")]
    public async Task<ActionResult<WeightItem>> AddWeight(int id, CreateWeightRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await herd.AddWeightAsync(id, request, cancellationToken));

    [HttpDelete("animals/{id:int}/weights/{weightId:int}")]
    public async Task<IActionResult> DeleteWeight(int id, int weightId, CancellationToken cancellationToken)
    {
        await herd.DeleteWeightAsync(id, weightId, cancellationToken);
        return NoContent();
    }

    [HttpGet("calf-sale-candidates")]
    public Task<IReadOnlyList<CalfSaleCandidateItem>> GetCalfSaleCandidates(CancellationToken cancellationToken) =>
        herd.GetCalfSaleCandidatesAsync(cancellationToken);
}
