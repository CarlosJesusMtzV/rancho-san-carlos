using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Reproduction;

public sealed class ReproductionService(TecNMDbContext db)
{
    private const int MaxOffspring = 6;
    private const int DueSoonDays = 30;

    /// <summary>Días de gestación usados para la fecha probable de parto.</summary>
    public static int GestationDays(short speciesId) => speciesId == Species.CattleId ? 283 : 150;

    // ------------------------------------------------------------------ Hembras

    public async Task<IReadOnlyList<FemaleStatus>> ListFemalesAsync(short? speciesId, string? status, CancellationToken cancellationToken)
    {
        var wanted = TextInput.Clean(status) ?? "all";
        if (wanted is not ("all" or "open" or "pending" or "pregnant" or "due-soon"))
        {
            throw ApiException.BadRequest("El estado debe ser all, open, pending, pregnant o due-soon.");
        }

        var females = db.Animals.AsNoTracking()
            .Where(animal => animal.Sex == "F" && animal.LifecycleStatus == AnimalLifecycle.Active);
        if (speciesId is { } species)
        {
            females = females.Where(animal => animal.SpeciesId == species);
        }

        var rows = await females
            .OrderBy(animal => animal.SpeciesId).ThenBy(animal => animal.VisualTag)
            .Select(animal => new
            {
                animal.Id,
                animal.VisualTag,
                animal.SpeciesId,
                BreedName = animal.Breed.Name,
                LotCode = animal.Lot != null ? animal.Lot.Code : null,
                animal.BirthDate,
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(row => row.Id).ToList();
        var latestBreedings = (await LoadBreedingsAsync(
                db.BreedingEvents.Where(breeding => ids.Contains(breeding.FemaleId)), cancellationToken))
            .GroupBy(breeding => breeding.FemaleId)
            .ToDictionary(group => group.Key, group => group.First());
        var births = (await db.BirthEvents.AsNoTracking()
                .Where(birth => ids.Contains(birth.DamId))
                .Select(birth => new { birth.DamId, birth.BirthDate })
                .ToListAsync(cancellationToken))
            .GroupBy(birth => birth.DamId)
            .ToDictionary(group => group.Key, group => (Last: group.Max(birth => birth.BirthDate), Count: group.Count()));

        var today = RanchClock.Today;
        var result = new List<FemaleStatus>();
        foreach (var row in rows)
        {
            var breeding = latestBreedings.GetValueOrDefault(row.Id);
            var inProgress = breeding is not null && BreedingOutcomes.InProgress.Contains(breeding.Outcome);
            int? daysToDue = inProgress ? breeding!.ExpectedDueDate.DayNumber - today.DayNumber : null;
            var matches = wanted switch
            {
                "open" => !inProgress,
                "pending" => inProgress && breeding!.Outcome == BreedingOutcomes.Pending,
                "pregnant" => inProgress && breeding!.Outcome == BreedingOutcomes.Pregnant,
                "due-soon" => inProgress && breeding!.Outcome == BreedingOutcomes.Pregnant && daysToDue <= DueSoonDays,
                _ => true,
            };
            if (!matches)
            {
                continue;
            }

            var birthStats = births.GetValueOrDefault(row.Id);
            result.Add(new FemaleStatus(
                row.Id,
                row.VisualTag,
                row.SpeciesId,
                row.BreedName,
                row.LotCode,
                AnimalAge.InMonths(row.BirthDate, today),
                breeding,
                birthStats.Count > 0 ? birthStats.Last : null,
                birthStats.Count,
                daysToDue));
        }

        return result;
    }

    // ------------------------------------------------------------------ Montas

    public async Task<IReadOnlyList<BreedingItem>> ListBreedingsAsync(
        int? femaleId, string? outcome, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (from > to)
        {
            throw ApiException.BadRequest("La fecha inicial no puede ser posterior a la final.");
        }

        var breedings = db.BreedingEvents.AsQueryable();
        if (femaleId is { } female)
        {
            breedings = breedings.Where(breeding => breeding.FemaleId == female);
        }

        if (TextInput.Clean(outcome) is { } wanted)
        {
            if (wanted is not (BreedingOutcomes.Pending or BreedingOutcomes.Pregnant or BreedingOutcomes.Open
                or BreedingOutcomes.Aborted or BreedingOutcomes.Delivered))
            {
                throw ApiException.BadRequest("El resultado de la monta no es válido.");
            }

            breedings = breedings.Where(breeding => breeding.Outcome == wanted);
        }

        if (from is { } start)
        {
            breedings = breedings.Where(breeding => breeding.MatingDate >= start);
        }

        if (to is { } end)
        {
            breedings = breedings.Where(breeding => breeding.MatingDate <= end);
        }

        return await LoadBreedingsAsync(breedings, cancellationToken);
    }

    public async Task<BreedingItem> CreateBreedingAsync(CreateBreedingRequest request, CancellationToken cancellationToken)
    {
        var matingDate = request.MatingDate ?? throw ApiException.BadRequest("Indica la fecha de la monta.");
        var notes = TextInput.Clean(request.Notes);
        if (notes?.Length > 255)
        {
            throw ApiException.BadRequest("Las notas admiten hasta 255 caracteres.");
        }

        if (request.FemaleId == request.MaleId)
        {
            throw ApiException.Unprocessable("Elige una hembra y un macho distintos.");
        }

        var pair = await db.Animals.AsNoTracking()
            .Where(animal => animal.Id == request.FemaleId || animal.Id == request.MaleId)
            .Select(animal => new { animal.Id, animal.VisualTag, animal.SpeciesId, animal.Sex, animal.LifecycleStatus, animal.BirthDate })
            .ToListAsync(cancellationToken);
        var female = pair.FirstOrDefault(animal => animal.Id == request.FemaleId) ?? throw ApiException.NotFound("La hembra no existe.");
        var male = pair.FirstOrDefault(animal => animal.Id == request.MaleId) ?? throw ApiException.NotFound("El macho no existe.");

        if (female.Sex != "F")
        {
            throw ApiException.Unprocessable($"{female.VisualTag} no es hembra.");
        }

        if (male.Sex != "M")
        {
            throw ApiException.Unprocessable($"{male.VisualTag} no es macho.");
        }

        if (female.SpeciesId != male.SpeciesId)
        {
            throw ApiException.Unprocessable("La hembra y el macho deben ser de la misma especie.");
        }

        foreach (var animal in new[] { female, male })
        {
            if (animal.LifecycleStatus != AnimalLifecycle.Active)
            {
                throw ApiException.Conflict($"{animal.VisualTag} ya no está activo en el hato.");
            }

            if (animal.BirthDate is { } birth && matingDate < birth)
            {
                throw ApiException.Unprocessable($"La fecha de monta es anterior al nacimiento de {animal.VisualTag}.");
            }
        }

        if (matingDate > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de monta no puede ser futura.");
        }

        var open = await db.BreedingEvents.AsNoTracking()
            .Where(breeding => breeding.FemaleId == female.Id && BreedingOutcomes.InProgress.Contains(breeding.Outcome))
            .Select(breeding => (DateOnly?)breeding.MatingDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (open is { } openDate)
        {
            throw ApiException.Conflict($"{female.VisualTag} ya tiene una monta del {openDate:dd/MM/yyyy} sin cerrar. Registra su diagnóstico o su parto primero.");
        }

        var breedingEvent = new BreedingEvent
        {
            SpeciesId = female.SpeciesId,
            FemaleId = female.Id,
            MaleId = male.Id,
            MatingDate = matingDate,
            ExpectedDueDate = matingDate.AddDays(GestationDays(female.SpeciesId)),
            Notes = notes,
        };
        db.BreedingEvents.Add(breedingEvent);
        await db.SaveChangesAsync(cancellationToken);
        return await GetBreedingAsync(breedingEvent.Id, cancellationToken);
    }

    public async Task DeleteBreedingAsync(int id, CancellationToken cancellationToken)
    {
        var breeding = await db.BreedingEvents.Include(item => item.Checks)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La monta no existe.");
        if (breeding.Outcome != BreedingOutcomes.Pending || breeding.Checks.Count > 0)
        {
            throw ApiException.Conflict("Solo se puede borrar una monta sin diagnóstico ni parto.");
        }

        breeding.DeletedAt = DateTime.UtcNow;
        breeding.Status = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BreedingItem> AddCheckAsync(int breedingId, CreateCheckRequest request, CancellationToken cancellationToken)
    {
        var result = TextInput.Clean(request.Result);
        if (result is null || !BreedingOutcomes.CheckResults.Contains(result))
        {
            throw ApiException.BadRequest("El diagnóstico debe ser cargada, vacía o aborto.");
        }

        var notes = TextInput.Clean(request.Notes);
        if (notes?.Length > 255)
        {
            throw ApiException.BadRequest("Las notas admiten hasta 255 caracteres.");
        }

        var breeding = await db.BreedingEvents.Include(item => item.Checks)
            .FirstOrDefaultAsync(item => item.Id == breedingId, cancellationToken)
            ?? throw ApiException.NotFound("La monta no existe.");
        if (breeding.Outcome == BreedingOutcomes.Delivered)
        {
            throw ApiException.Conflict("La monta ya terminó en parto.");
        }

        var visitDate = await db.VetVisits.Where(visit => visit.Id == request.VetVisitId)
            .Select(visit => (DateOnly?)visit.VisitDate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw ApiException.NotFound("La visita no existe.");
        if (visitDate <= breeding.MatingDate)
        {
            throw ApiException.Unprocessable($"La visita debe ser posterior a la monta del {breeding.MatingDate:dd/MM/yyyy}.");
        }

        if (breeding.Checks.Any(check => check.VetVisitId == request.VetVisitId))
        {
            throw ApiException.Conflict("Esa visita ya tiene un diagnóstico para esta monta.");
        }

        breeding.Checks.Add(new PregnancyCheck
        {
            VetVisitId = request.VetVisitId,
            CheckDate = visitDate,
            Result = result,
            Notes = notes,
        });

        // El estado de la monta es el del diagnóstico más reciente por fecha de visita;
        // si coinciden, gana el que se acaba de registrar (aún sin Id).
        var latest = breeding.Checks.MaxBy(check => (check.CheckDate, check.Id == 0 ? int.MaxValue : check.Id))!;
        breeding.Outcome = latest.Result;
        await db.SaveChangesAsync(cancellationToken);
        return await GetBreedingAsync(breeding.Id, cancellationToken);
    }

    // ------------------------------------------------------------------ Partos

    public async Task<IReadOnlyList<BirthItem>> ListBirthsAsync(int? damId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (from > to)
        {
            throw ApiException.BadRequest("La fecha inicial no puede ser posterior a la final.");
        }

        var births = db.BirthEvents.AsQueryable();
        if (damId is { } dam)
        {
            births = births.Where(birth => birth.DamId == dam);
        }

        if (from is { } start)
        {
            births = births.Where(birth => birth.BirthDate >= start);
        }

        if (to is { } end)
        {
            births = births.Where(birth => birth.BirthDate <= end);
        }

        return await LoadBirthsAsync(births, cancellationToken);
    }

    public async Task<BirthItem> CreateBirthAsync(CreateBirthRequest request, CancellationToken cancellationToken)
    {
        var birthDate = request.BirthDate ?? throw ApiException.BadRequest("Indica la fecha del parto.");
        var stillborn = request.StillbornCount ?? 0;
        var offspring = request.Offspring ?? [];
        var complications = TextInput.Clean(request.Complications);
        if (stillborn < 0 || offspring.Count + stillborn is 0 or > MaxOffspring)
        {
            throw ApiException.BadRequest($"Un parto tiene de 1 a {MaxOffspring} crías entre vivas y muertas.");
        }

        if (complications?.Length > 255)
        {
            throw ApiException.BadRequest("Las complicaciones admiten hasta 255 caracteres.");
        }

        if (birthDate > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha del parto no puede ser futura.");
        }

        var dam = await db.Animals.AsNoTracking().FirstOrDefaultAsync(animal => animal.Id == request.DamId, cancellationToken)
            ?? throw ApiException.NotFound("La madre no existe.");
        if (dam.Sex != "F")
        {
            throw ApiException.Unprocessable($"{dam.VisualTag} no es hembra.");
        }

        if (dam.LifecycleStatus != AnimalLifecycle.Active)
        {
            throw ApiException.Conflict($"{dam.VisualTag} ya no está activa en el hato.");
        }

        if (dam.BirthDate is { } damBirth && birthDate <= damBirth)
        {
            throw ApiException.Unprocessable("La fecha del parto debe ser posterior al nacimiento de la madre.");
        }

        var breeding = await db.BreedingEvents
            .Where(item => item.FemaleId == dam.Id && BreedingOutcomes.InProgress.Contains(item.Outcome))
            .OrderByDescending(item => item.MatingDate)
            .FirstOrDefaultAsync(cancellationToken);
        if (breeding is not null && birthDate <= breeding.MatingDate)
        {
            throw ApiException.Unprocessable($"La fecha del parto debe ser posterior a la monta del {breeding.MatingDate:dd/MM/yyyy}.");
        }

        var birth = new BirthEvent
        {
            DamId = dam.Id,
            BreedingEventId = breeding?.Id,
            BirthDate = birthDate,
            OffspringCount = (short)offspring.Count,
            StillbornCount = stillborn,
            Complications = complications,
        };

        var tags = new HashSet<string>();
        foreach (var (input, index) in offspring.Select((input, index) => (input, index + 1)))
        {
            birth.Offspring.Add(await BuildOffspringAsync(input, index, dam, breeding?.MaleId, birthDate, tags, cancellationToken));
        }

        if (breeding is not null)
        {
            breeding.Outcome = BreedingOutcomes.Delivered;
        }

        // Una sola llamada: el parto, las crías y la monta se guardan juntos o nada.
        db.BirthEvents.Add(birth);
        await db.SaveChangesAsync(cancellationToken);
        return (await LoadBirthsAsync(db.BirthEvents.Where(item => item.Id == birth.Id), cancellationToken)).Single();
    }

    private async Task<Animal> BuildOffspringAsync(
        OffspringInput input, int index, Animal dam, int? sireId, DateOnly birthDate, HashSet<string> tags, CancellationToken cancellationToken)
    {
        var label = $"Cría {index}";
        var visualTag = TextInput.CleanCode(input.VisualTag) ?? throw ApiException.BadRequest($"{label}: escribe su arete visual.");
        var officialTag = TextInput.CleanCode(input.OfficialTag);
        if (visualTag.Length > 30 || officialTag?.Length > 30)
        {
            throw ApiException.BadRequest($"{label}: los aretes admiten hasta 30 caracteres.");
        }

        var sex = TextInput.Clean(input.Sex)?.ToUpperInvariant();
        if (sex is not ("M" or "F"))
        {
            throw ApiException.BadRequest($"{label}: indica si es macho o hembra.");
        }

        if (input.BirthWeightKg is { } weight && (weight is < 0.1m or > 100m || !TextInput.HasTwoDecimalsAtMost(weight)))
        {
            throw ApiException.Unprocessable($"{label}: el peso al nacer va de 0.1 a 100 kg con dos decimales.");
        }

        if (!tags.Add(visualTag) || (officialTag is not null && !tags.Add($"#{officialTag}")))
        {
            throw ApiException.Conflict($"{label}: el arete se repite en este parto.");
        }

        if (await db.Animals.IgnoreQueryFilters().AnyAsync(animal => animal.VisualTag == visualTag, cancellationToken))
        {
            throw ApiException.Conflict($"{label}: ya existe un animal con el arete visual {visualTag}.");
        }

        if (officialTag is not null && await db.Animals.IgnoreQueryFilters().AnyAsync(animal => animal.OfficialTag == officialTag, cancellationToken))
        {
            throw ApiException.Conflict($"{label}: ya existe un animal con el arete oficial {officialTag}.");
        }

        var breedId = input.BreedId ?? dam.BreedId;
        if (input.BreedId is not null && !await db.Breeds.AnyAsync(breed => breed.Id == breedId && breed.SpeciesId == dam.SpeciesId, cancellationToken))
        {
            throw ApiException.Unprocessable($"{label}: la raza no corresponde a la especie de la madre.");
        }

        var lotId = input.LotId ?? dam.LotId;
        if (input.LotId is not null && !await db.AnimalLots.AnyAsync(lot => lot.Id == lotId && lot.SpeciesId == dam.SpeciesId, cancellationToken))
        {
            throw ApiException.Unprocessable($"{label}: el lote no corresponde a la especie de la madre.");
        }

        return new Animal
        {
            VisualTag = visualTag,
            OfficialTag = officialTag,
            SpeciesId = dam.SpeciesId,
            BreedId = breedId,
            LotId = lotId,
            Sex = sex,
            BirthDate = birthDate,
            ProductionStage = dam.SpeciesId == Species.CattleId ? "calf" : "kid",
            Origin = AnimalOrigins.BornOnRanch,
            BirthWeightKg = input.BirthWeightKg,
            SireId = sireId,
            DamId = dam.Id,
        };
    }

    // ------------------------------------------------------------------ Lectura

    private async Task<BreedingItem> GetBreedingAsync(int id, CancellationToken cancellationToken) =>
        (await LoadBreedingsAsync(db.BreedingEvents.Where(breeding => breeding.Id == id), cancellationToken)).Single();

    private async Task<List<BreedingItem>> LoadBreedingsAsync(IQueryable<BreedingEvent> query, CancellationToken cancellationToken)
    {
        var breedings = await query.AsNoTracking()
            .Include(breeding => breeding.Female)
            .Include(breeding => breeding.Male)
            .Include(breeding => breeding.Checks).ThenInclude(check => check.VetVisit)
            .OrderByDescending(breeding => breeding.MatingDate).ThenByDescending(breeding => breeding.Id)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var ids = breedings.Select(breeding => breeding.Id).ToList();
        var births = await db.BirthEvents.AsNoTracking()
            .Where(birth => birth.BreedingEventId != null && ids.Contains(birth.BreedingEventId.Value))
            .ToDictionaryAsync(birth => birth.BreedingEventId!.Value, birth => birth.Id, cancellationToken);

        return breedings.Select(breeding => new BreedingItem(
                breeding.Id,
                breeding.SpeciesId,
                breeding.FemaleId,
                breeding.Female.VisualTag,
                breeding.MaleId,
                breeding.Male.VisualTag,
                breeding.MatingDate,
                breeding.ExpectedDueDate,
                breeding.Outcome,
                breeding.Notes,
                breeding.Checks
                    .OrderBy(check => check.CheckDate).ThenBy(check => check.Id)
                    .Select(check => new PregnancyCheckItem(check.Id, check.VetVisitId, check.VetVisit.VetName, check.CheckDate, check.Result, check.Notes))
                    .ToList(),
                births.TryGetValue(breeding.Id, out var birthId) ? birthId : null))
            .ToList();
    }

    private static async Task<List<BirthItem>> LoadBirthsAsync(IQueryable<BirthEvent> query, CancellationToken cancellationToken)
    {
        var births = await query.AsNoTracking()
            .Include(birth => birth.Dam)
            .Include(birth => birth.BreedingEvent).ThenInclude(breeding => breeding!.Male)
            .Include(birth => birth.Offspring)
            .OrderByDescending(birth => birth.BirthDate).ThenByDescending(birth => birth.Id)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return births.Select(birth => new BirthItem(
                birth.Id,
                birth.DamId,
                birth.Dam.VisualTag,
                birth.Dam.SpeciesId,
                birth.BreedingEventId,
                birth.BreedingEvent?.Male.VisualTag,
                birth.BirthDate,
                birth.OffspringCount,
                birth.StillbornCount,
                birth.Complications,
                birth.Offspring
                    .OrderBy(animal => animal.VisualTag)
                    .Select(animal => new OffspringItem(animal.Id, animal.VisualTag, animal.Sex, animal.BirthWeightKg))
                    .ToList()))
            .ToList();
    }

}
