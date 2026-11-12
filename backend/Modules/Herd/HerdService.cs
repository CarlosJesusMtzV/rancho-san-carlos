using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Herd;

public sealed class HerdService(TecNMDbContext db)
{
    private const int MaxPageSize = 200;

    // ------------------------------------------------------------------ Catálogos

    public async Task<HerdCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var species = await db.Species.AsNoTracking()
            .OrderBy(item => item.Id)
            .Select(item => new SpeciesItem(item.Id, item.Name, item.ProductionPurpose))
            .ToListAsync(cancellationToken);

        var breeds = await db.Breeds.AsNoTracking()
            .OrderBy(item => item.SpeciesId).ThenBy(item => item.Name)
            .Select(item => new BreedItem(item.Id, item.SpeciesId, item.Name, item.Purpose))
            .ToListAsync(cancellationToken);

        var lots = await db.AnimalLots.AsNoTracking()
            .OrderBy(item => item.SpeciesId).ThenBy(item => item.Code)
            .Select(item => new LotItem(
                item.Id,
                item.SpeciesId,
                item.Code,
                item.Description,
                db.Animals.Count(animal => animal.LotId == item.Id && animal.LifecycleStatus == AnimalLifecycle.Active)))
            .ToListAsync(cancellationToken);

        return new HerdCatalog(species, breeds, lots);
    }

    public async Task<BreedItem> CreateBreedAsync(CreateBreedRequest request, CancellationToken cancellationToken)
    {
        await EnsureSpeciesAsync(request.SpeciesId, cancellationToken);
        var name = HerdRules.Clean(request.Name) ?? throw ApiException.BadRequest("Escribe el nombre de la raza.");
        if (name.Length > 80)
        {
            throw ApiException.BadRequest("El nombre de la raza admite hasta 80 caracteres.");
        }

        var purpose = HerdRules.Clean(request.Purpose);
        if (purpose?.Length > 120)
        {
            throw ApiException.BadRequest("El propósito admite hasta 120 caracteres.");
        }

        var lowered = name.ToLowerInvariant();
        var exists = await db.Breeds.IgnoreQueryFilters()
            .AnyAsync(item => item.SpeciesId == request.SpeciesId && item.Name.ToLower() == lowered, cancellationToken);
        if (exists)
        {
            throw ApiException.Conflict($"La raza «{name}» ya está registrada para esa especie.");
        }

        var breed = new Breed { SpeciesId = request.SpeciesId, Name = name, Purpose = purpose };
        db.Breeds.Add(breed);
        await db.SaveChangesAsync(cancellationToken);
        return new BreedItem(breed.Id, breed.SpeciesId, breed.Name, breed.Purpose);
    }

    public async Task<LotItem> CreateLotAsync(CreateLotRequest request, CancellationToken cancellationToken)
    {
        await EnsureSpeciesAsync(request.SpeciesId, cancellationToken);
        var code = HerdRules.CleanCode(request.Code) ?? throw ApiException.BadRequest("Escribe el código del lote.");
        if (code.Length > 30)
        {
            throw ApiException.BadRequest("El código del lote admite hasta 30 caracteres.");
        }

        var description = HerdRules.Clean(request.Description);
        if (description?.Length > 255)
        {
            throw ApiException.BadRequest("La descripción admite hasta 255 caracteres.");
        }

        if (await db.AnimalLots.IgnoreQueryFilters().AnyAsync(item => item.Code == code, cancellationToken))
        {
            throw ApiException.Conflict($"El lote «{code}» ya existe.");
        }

        var lot = new AnimalLot { SpeciesId = request.SpeciesId, Code = code, Description = description };
        db.AnimalLots.Add(lot);
        await db.SaveChangesAsync(cancellationToken);
        return new LotItem(lot.Id, lot.SpeciesId, lot.Code, lot.Description, 0);
    }

    // ------------------------------------------------------------------ Animales

    public async Task<AnimalPage> ListAnimalsAsync(AnimalQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var animals = db.Animals.AsNoTracking();

        if (query.SpeciesId is { } speciesId)
        {
            animals = animals.Where(animal => animal.SpeciesId == speciesId);
        }

        if (query.LotId is { } lotId)
        {
            animals = animals.Where(animal => animal.LotId == lotId);
        }

        if (HerdRules.Clean(query.Sex) is { } sex)
        {
            if (sex is not ("M" or "F"))
            {
                throw ApiException.BadRequest("El sexo debe ser M o F.");
            }

            animals = animals.Where(animal => animal.Sex == sex);
        }

        animals = (HerdRules.Clean(query.Status) ?? "active") switch
        {
            "active" => animals.Where(animal => animal.LifecycleStatus == AnimalLifecycle.Active),
            "inactive" => animals.Where(animal => animal.LifecycleStatus != AnimalLifecycle.Active),
            "all" => animals,
            _ => throw ApiException.BadRequest("El estado debe ser active, inactive o all."),
        };

        if (HerdRules.Clean(query.Search) is { } search)
        {
            var pattern = $"%{search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            animals = animals.Where(animal =>
                EF.Functions.ILike(animal.VisualTag, pattern) ||
                (animal.OfficialTag != null && EF.Functions.ILike(animal.OfficialTag, pattern)));
        }

        var total = await animals.CountAsync(cancellationToken);
        var rows = await animals
            .OrderBy(animal => animal.SpeciesId).ThenBy(animal => animal.VisualTag)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(animal => new
            {
                animal.Id,
                animal.VisualTag,
                animal.OfficialTag,
                animal.SpeciesId,
                SpeciesName = animal.Species.Name,
                BreedName = animal.Breed.Name,
                animal.LotId,
                LotCode = animal.Lot != null ? animal.Lot.Code : null,
                animal.Sex,
                animal.BirthDate,
                animal.ProductionStage,
                animal.LifecycleStatus,
                LastWeight = animal.Weights
                    .OrderByDescending(weight => weight.WeighedOn)
                    .Select(weight => new { weight.WeightKg, weight.WeighedOn })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var today = RanchClock.Today;
        var items = rows.Select(row => new AnimalSummary(
                row.Id,
                row.VisualTag,
                row.OfficialTag,
                row.SpeciesId,
                row.SpeciesName,
                row.BreedName,
                row.LotId,
                row.LotCode,
                row.Sex,
                row.BirthDate,
                HerdRules.AgeInMonths(row.BirthDate, today),
                row.ProductionStage,
                row.LifecycleStatus,
                row.LastWeight?.WeightKg,
                row.LastWeight?.WeighedOn))
            .ToList();

        return new AnimalPage(items, total, page, pageSize);
    }

    public async Task<AnimalDetail> GetAnimalAsync(int id, CancellationToken cancellationToken)
    {
        var animal = await db.Animals.AsNoTracking()
            .Include(item => item.Species)
            .Include(item => item.Breed)
            .Include(item => item.Lot)
            .Include(item => item.Sire)
            .Include(item => item.Dam)
            .Include(item => item.Weights)
            .AsSplitQuery()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw NotFound();

        return ToDetail(animal);
    }

    public async Task<AnimalDetail> CreateAnimalAsync(CreateAnimalRequest request, CancellationToken cancellationToken)
    {
        await EnsureSpeciesAsync(request.SpeciesId, cancellationToken);
        var animal = new Animal { SpeciesId = request.SpeciesId };
        await ApplyAsync(animal, new AnimalFields(
            request.VisualTag, request.OfficialTag, request.BreedId, request.LotId, request.Sex, request.BirthDate,
            request.ProductionStage, request.Origin, request.BirthWeightKg, request.SireId, request.DamId,
            request.GeneticsNotes), cancellationToken);

        db.Animals.Add(animal);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAnimalAsync(animal.Id, cancellationToken);
    }

    public async Task<AnimalDetail> UpdateAnimalAsync(int id, UpdateAnimalRequest request, CancellationToken cancellationToken)
    {
        var animal = await db.Animals.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw NotFound();
        await ApplyAsync(animal, new AnimalFields(
            request.VisualTag, request.OfficialTag, request.BreedId, request.LotId, request.Sex, request.BirthDate,
            request.ProductionStage, request.Origin, request.BirthWeightKg, request.SireId, request.DamId,
            request.GeneticsNotes), cancellationToken);

        if (animal.ExitDate is { } exitDate && animal.BirthDate > exitDate)
        {
            throw ApiException.Unprocessable("La fecha de nacimiento no puede ser posterior a la fecha de salida.");
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAnimalAsync(animal.Id, cancellationToken);
    }

    public async Task<AnimalDetail> RegisterExitAsync(int id, AnimalExitRequest request, CancellationToken cancellationToken)
    {
        var status = HerdRules.Clean(request.Status);
        if (status is null || !HerdRules.ManualExitStatuses.Contains(status))
        {
            throw ApiException.BadRequest("El motivo de salida debe ser vendido, muerto o desecho.");
        }

        var exitDate = request.ExitDate ?? throw ApiException.BadRequest("Indica la fecha de salida.");
        var animal = await db.Animals.FirstOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw NotFound();
        EnsureActive(animal);
        EnsureDateInLife(exitDate, animal.BirthDate, "de salida");

        animal.LifecycleStatus = status;
        animal.ExitDate = exitDate;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAnimalAsync(animal.Id, cancellationToken);
    }

    // ------------------------------------------------------------------ Pesajes

    public async Task<WeightItem> AddWeightAsync(int animalId, CreateWeightRequest request, CancellationToken cancellationToken)
    {
        var weighedOn = request.WeighedOn ?? throw ApiException.BadRequest("Indica la fecha del pesaje.");
        var weightKg = request.WeightKg ?? throw ApiException.BadRequest("Indica el peso en kilogramos.");
        if (weightKg < HerdRules.MinWeightKg || weightKg > HerdRules.MaxWeightKg)
        {
            throw ApiException.Unprocessable($"El peso debe estar entre {HerdRules.MinWeightKg} y {HerdRules.MaxWeightKg:0} kg.");
        }

        if (decimal.Round(weightKg, 2) != weightKg)
        {
            throw ApiException.BadRequest("El peso admite hasta dos decimales.");
        }

        var notes = HerdRules.Clean(request.Notes);
        if (notes?.Length > 255)
        {
            throw ApiException.BadRequest("Las notas admiten hasta 255 caracteres.");
        }

        var animal = await db.Animals.AsNoTracking().FirstOrDefaultAsync(item => item.Id == animalId, cancellationToken)
            ?? throw NotFound();
        EnsureActive(animal);
        EnsureDateInLife(weighedOn, animal.BirthDate, "del pesaje");

        // La llave única (animal_id, weighed_on) incluye pesajes eliminados: si existe uno borrado, se reactiva.
        var existing = await db.WeightRecords.IgnoreQueryFilters()
            .FirstOrDefaultAsync(item => item.AnimalId == animalId && item.WeighedOn == weighedOn, cancellationToken);

        if (existing is { DeletedAt: null })
        {
            throw ApiException.Conflict($"Ya hay un pesaje el {weighedOn:dd/MM/yyyy}. Elimínalo si quieres corregirlo.");
        }

        var weight = existing ?? new WeightRecord { AnimalId = animalId, WeighedOn = weighedOn };
        weight.WeightKg = weightKg;
        weight.Notes = notes;
        weight.DeletedAt = null;
        weight.DeletedBy = null;
        weight.Status = 1;
        if (existing is null)
        {
            db.WeightRecords.Add(weight);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new WeightItem(weight.Id, weight.WeighedOn, weight.WeightKg, weight.Notes);
    }

    public async Task DeleteWeightAsync(int animalId, int weightId, CancellationToken cancellationToken)
    {
        var weight = await db.WeightRecords
            .FirstOrDefaultAsync(item => item.Id == weightId && item.AnimalId == animalId, cancellationToken)
            ?? throw ApiException.NotFound("El pesaje no existe.");

        weight.DeletedAt = DateTime.UtcNow;
        weight.Status = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ Becerros para venta

    public async Task<IReadOnlyList<CalfSaleCandidateItem>> GetCalfSaleCandidatesAsync(CancellationToken cancellationToken)
    {
        var lots = await db.AnimalLots.AsNoTracking()
            .ToDictionaryAsync(lot => lot.Id, lot => lot.Code, cancellationToken);

        var rows = await db.CalfSaleCandidates.AsNoTracking()
            .OrderByDescending(row => row.ReachedSaleTarget)
            .ThenByDescending(row => row.AgeMonths)
            .ThenBy(row => row.VisualTag)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new CalfSaleCandidateItem(
                row.AnimalId,
                row.VisualTag,
                row.OfficialTag,
                row.Sex,
                row.LotId is { } lotId && lots.TryGetValue(lotId, out var code) ? code : null,
                row.BirthDate,
                row.AgeMonths,
                row.LastWeightKg,
                row.LastWeighedOn,
                row.MeatAllowedFrom,
                row.ReachedSaleTarget))
            .ToList();
    }

    // ------------------------------------------------------------------ Validación compartida

    private sealed record AnimalFields(
        string? VisualTag,
        string? OfficialTag,
        int BreedId,
        int? LotId,
        string? Sex,
        DateOnly? BirthDate,
        string? ProductionStage,
        string? Origin,
        decimal? BirthWeightKg,
        int? SireId,
        int? DamId,
        string? GeneticsNotes);

    private async Task ApplyAsync(Animal animal, AnimalFields fields, CancellationToken cancellationToken)
    {
        var visualTag = HerdRules.CleanCode(fields.VisualTag) ?? throw ApiException.BadRequest("Escribe el arete visual.");
        var officialTag = HerdRules.CleanCode(fields.OfficialTag);
        if (visualTag.Length > 30 || officialTag?.Length > 30)
        {
            throw ApiException.BadRequest("Los aretes admiten hasta 30 caracteres.");
        }

        var sex = HerdRules.Clean(fields.Sex)?.ToUpperInvariant();
        if (sex is not ("M" or "F"))
        {
            throw ApiException.BadRequest("El sexo debe ser M (macho) o F (hembra).");
        }

        var origin = HerdRules.Clean(fields.Origin) ?? AnimalOrigins.BornOnRanch;
        if (origin is not (AnimalOrigins.BornOnRanch or AnimalOrigins.Purchased))
        {
            throw ApiException.BadRequest("El origen debe ser nacido en el rancho o comprado.");
        }

        var notes = HerdRules.Clean(fields.GeneticsNotes);
        if (notes?.Length > 2000)
        {
            throw ApiException.BadRequest("Las notas genéticas admiten hasta 2000 caracteres.");
        }

        if (fields.BirthDate > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de nacimiento no puede ser futura.");
        }

        if (fields.BirthWeightKg is { } birthWeight && (birthWeight <= 0 || birthWeight > 100 || decimal.Round(birthWeight, 2) != birthWeight))
        {
            throw ApiException.Unprocessable("El peso al nacer debe ser mayor que 0 y de hasta 100 kg, con dos decimales como máximo.");
        }

        var stage = HerdRules.Clean(fields.ProductionStage);
        if (stage is not null && !HerdRules.StageExists(animal.SpeciesId, stage))
        {
            throw ApiException.Unprocessable("La etapa productiva no corresponde a la especie.");
        }

        if (stage is not null && !HerdRules.IsValidStage(animal.SpeciesId, stage, sex))
        {
            throw ApiException.Unprocessable("La etapa productiva no corresponde al sexo del animal.");
        }

        var breedSpecies = await db.Breeds.AsNoTracking()
            .Where(breed => breed.Id == fields.BreedId)
            .Select(breed => (short?)breed.SpeciesId)
            .FirstOrDefaultAsync(cancellationToken);
        if (breedSpecies is null)
        {
            throw ApiException.Unprocessable("Selecciona una raza registrada.");
        }

        if (breedSpecies != animal.SpeciesId)
        {
            throw ApiException.Unprocessable("La raza no corresponde a la especie del animal.");
        }

        if (fields.LotId is { } lotId)
        {
            var lotSpecies = await db.AnimalLots.AsNoTracking()
                .Where(lot => lot.Id == lotId)
                .Select(lot => (short?)lot.SpeciesId)
                .FirstOrDefaultAsync(cancellationToken);
            if (lotSpecies is null)
            {
                throw ApiException.Unprocessable("Selecciona un lote registrado.");
            }

            if (lotSpecies != animal.SpeciesId)
            {
                throw ApiException.Unprocessable("El lote no corresponde a la especie del animal.");
            }
        }

        await EnsureParentAsync(animal, fields.SireId, "M", "padre", cancellationToken);
        await EnsureParentAsync(animal, fields.DamId, "F", "madre", cancellationToken);

        if (await db.Animals.IgnoreQueryFilters()
                .AnyAsync(item => item.VisualTag == visualTag && item.Id != animal.Id, cancellationToken))
        {
            throw ApiException.Conflict($"Ya existe un animal con el arete visual {visualTag}.");
        }

        if (officialTag is not null && await db.Animals.IgnoreQueryFilters()
                .AnyAsync(item => item.OfficialTag == officialTag && item.Id != animal.Id, cancellationToken))
        {
            throw ApiException.Conflict($"Ya existe un animal con el arete oficial {officialTag}.");
        }

        animal.VisualTag = visualTag;
        animal.OfficialTag = officialTag;
        animal.BreedId = fields.BreedId;
        animal.LotId = fields.LotId;
        animal.Sex = sex;
        animal.BirthDate = fields.BirthDate;
        animal.ProductionStage = stage;
        animal.Origin = origin;
        animal.BirthWeightKg = fields.BirthWeightKg;
        animal.SireId = fields.SireId;
        animal.DamId = fields.DamId;
        animal.GeneticsNotes = notes;
    }

    private async Task EnsureParentAsync(Animal animal, int? parentId, string sex, string role, CancellationToken cancellationToken)
    {
        if (parentId is not { } id)
        {
            return;
        }

        if (id == animal.Id)
        {
            throw ApiException.Unprocessable(role == "padre"
                ? "Un animal no puede ser su propio padre."
                : "Un animal no puede ser su propia madre.");
        }

        var parent = await db.Animals.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new { item.SpeciesId, item.Sex })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw ApiException.Unprocessable($"El {role} seleccionado no existe.");

        if (parent.SpeciesId != animal.SpeciesId)
        {
            throw ApiException.Unprocessable($"El {role} debe ser de la misma especie.");
        }

        if (parent.Sex != sex)
        {
            throw ApiException.Unprocessable(sex == "M" ? "El padre debe ser un macho." : "La madre debe ser una hembra.");
        }
    }

    private async Task EnsureSpeciesAsync(short speciesId, CancellationToken cancellationToken)
    {
        if (!await db.Species.AnyAsync(item => item.Id == speciesId, cancellationToken))
        {
            throw ApiException.BadRequest("La especie debe ser Bovino (1) o Caprino (2).");
        }
    }

    private static void EnsureActive(Animal animal)
    {
        if (animal.LifecycleStatus != AnimalLifecycle.Active)
        {
            throw ApiException.Conflict($"El animal {animal.VisualTag} ya no está activo en el hato.");
        }
    }

    private static void EnsureDateInLife(DateOnly date, DateOnly? birthDate, string label)
    {
        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable($"La fecha {label} no puede ser futura.");
        }

        if (birthDate is { } birth && date < birth)
        {
            throw ApiException.Unprocessable($"La fecha {label} no puede ser anterior al nacimiento.");
        }
    }

    private static ApiException NotFound() => ApiException.NotFound("El animal no existe.");

    private static AnimalDetail ToDetail(Animal animal)
    {
        var weights = animal.Weights
            .OrderByDescending(weight => weight.WeighedOn)
            .Select(weight => new WeightItem(weight.Id, weight.WeighedOn, weight.WeightKg, weight.Notes))
            .ToList();
        var last = weights.FirstOrDefault();

        return new AnimalDetail(
            animal.Id,
            animal.VisualTag,
            animal.OfficialTag,
            animal.SpeciesId,
            animal.Species.Name,
            animal.BreedId,
            animal.Breed.Name,
            animal.LotId,
            animal.Lot?.Code,
            animal.Sex,
            animal.BirthDate,
            HerdRules.AgeInMonths(animal.BirthDate, RanchClock.Today),
            animal.ProductionStage,
            animal.Origin,
            animal.BirthWeightKg,
            animal.LifecycleStatus,
            animal.ExitDate,
            animal.GeneticsNotes,
            animal.Sire is null ? null : new AnimalRef(animal.Sire.Id, animal.Sire.VisualTag),
            animal.Dam is null ? null : new AnimalRef(animal.Dam.Id, animal.Dam.VisualTag),
            last?.WeightKg,
            last?.WeighedOn,
            weights);
    }
}
