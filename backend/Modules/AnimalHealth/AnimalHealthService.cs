using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.AnimalHealth;

public sealed class AnimalHealthService(TecNMDbContext db)
{
    private const int MaxAnimalsPerRecord = 500;
    private const int MaxTreatments = 10;
    private const short MaxWithdrawalDays = 365;
    private const decimal MaxAmount = 1_000_000m;
    private const int MaxPageSize = 200;

    private static readonly string[] HealthCategories = [SupplyCategories.Medicine, SupplyCategories.Vaccine];

    // ------------------------------------------------------------------ Catálogo de medicamentos

    public async Task<HealthCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var types = await db.SupplyTypes.AsNoTracking()
            .Where(type => HealthCategories.Contains(type.Category))
            .OrderBy(type => type.Id)
            .Select(type => new SupplyTypeItem(type.Id, type.Name, type.Category))
            .ToListAsync(cancellationToken);

        var medications = await MedicationsQuery()
            .OrderBy(supply => supply.CommercialName)
            .Select(ToMedicationItem)
            .ToListAsync(cancellationToken);

        return new HealthCatalog(types, medications);
    }

    public async Task<MedicationItem> CreateMedicationAsync(MedicationInput input, CancellationToken cancellationToken)
    {
        var supply = new Supply();
        await ApplyMedicationAsync(supply, input, cancellationToken);
        db.Supplies.Add(supply);
        await db.SaveChangesAsync(cancellationToken);
        return await GetMedicationAsync(supply.Id, cancellationToken);
    }

    public async Task<MedicationItem> UpdateMedicationAsync(int id, MedicationInput input, CancellationToken cancellationToken)
    {
        var supply = await db.Supplies
            .FirstOrDefaultAsync(item => item.Id == id && HealthCategories.Contains(item.SupplyType.Category), cancellationToken)
            ?? throw ApiException.NotFound("El medicamento no existe.");
        await ApplyMedicationAsync(supply, input, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetMedicationAsync(supply.Id, cancellationToken);
    }

    private async Task ApplyMedicationAsync(Supply supply, MedicationInput input, CancellationToken cancellationToken)
    {
        var name = TextInput.Clean(input.CommercialName) ?? throw ApiException.BadRequest("Escribe el nombre comercial del medicamento.");
        var ingredient = TextInput.Clean(input.ActiveIngredient);
        if (name.Length > 120 || ingredient?.Length > 120)
        {
            throw ApiException.BadRequest("El nombre y el principio activo admiten hasta 120 caracteres.");
        }

        var milkDays = input.DefaultMilkWithdrawalDays ?? 0;
        var meatDays = input.DefaultMeatWithdrawalDays ?? 0;
        EnsureWithdrawalDays(milkDays, meatDays);

        var isHealthType = await db.SupplyTypes.AnyAsync(
            type => type.Id == input.SupplyTypeId && HealthCategories.Contains(type.Category), cancellationToken);
        if (!isHealthType)
        {
            throw ApiException.Unprocessable("Elige un tipo de sanidad: medicamento, vacuna o desparasitante.");
        }

        var lowered = name.ToLowerInvariant();
        var duplicated = await db.Supplies.IgnoreQueryFilters()
            .AnyAsync(item => item.Id != supply.Id && item.CommercialName.ToLower() == lowered, cancellationToken);
        if (duplicated)
        {
            throw ApiException.Conflict($"Ya existe un insumo llamado «{name}».");
        }

        supply.SupplyTypeId = input.SupplyTypeId;
        supply.CommercialName = name;
        supply.ActiveIngredient = ingredient;
        supply.DefaultMilkWithdrawalDays = milkDays;
        supply.DefaultMeatWithdrawalDays = meatDays;
    }

    private IQueryable<Supply> MedicationsQuery() =>
        db.Supplies.AsNoTracking().Where(supply => HealthCategories.Contains(supply.SupplyType.Category));

    private static readonly System.Linq.Expressions.Expression<Func<Supply, MedicationItem>> ToMedicationItem = supply =>
        new MedicationItem(
            supply.Id,
            supply.SupplyTypeId,
            supply.SupplyType.Name,
            supply.CommercialName,
            supply.ActiveIngredient,
            supply.DefaultMilkWithdrawalDays,
            supply.DefaultMeatWithdrawalDays);

    private async Task<MedicationItem> GetMedicationAsync(int id, CancellationToken cancellationToken) =>
        await MedicationsQuery().Where(supply => supply.Id == id).Select(ToMedicationItem).FirstAsync(cancellationToken);

    // ------------------------------------------------------------------ Visitas del veterinario

    public async Task<IReadOnlyList<VetVisitSummary>> ListVisitsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        EnsureRange(from, to);
        var visits = db.VetVisits.AsNoTracking();
        if (from is { } start)
        {
            visits = visits.Where(visit => visit.VisitDate >= start);
        }

        if (to is { } end)
        {
            visits = visits.Where(visit => visit.VisitDate <= end);
        }

        return await visits
            .OrderByDescending(visit => visit.VisitDate).ThenByDescending(visit => visit.Id)
            .Select(visit => new VetVisitSummary(
                visit.Id,
                visit.VisitDate,
                visit.VetName,
                visit.ServiceFee,
                visit.MedicationCost,
                visit.ServiceFee + visit.MedicationCost,
                visit.Notes,
                visit.Events.Count(),
                visit.Events.Select(item => item.AnimalId).Distinct().Count()))
            .ToListAsync(cancellationToken);
    }

    public async Task<VetVisitDetail> GetVisitAsync(int id, CancellationToken cancellationToken)
    {
        var visit = await db.VetVisits.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La visita no existe.");
        var events = await LoadEventsAsync(EventsQuery().Where(item => item.VetVisitId == id), cancellationToken);

        return new VetVisitDetail(
            visit.Id,
            visit.VisitDate,
            visit.VetName,
            visit.ServiceFee,
            visit.MedicationCost,
            visit.ServiceFee + visit.MedicationCost,
            visit.Notes,
            events.Count,
            events.Select(item => item.AnimalId).Distinct().Count(),
            events);
    }

    public async Task<VetVisitDetail> CreateVisitAsync(VetVisitInput input, CancellationToken cancellationToken)
    {
        var visit = new VetVisit();
        ApplyVisit(visit, input);
        db.VetVisits.Add(visit);
        await db.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visit.Id, cancellationToken);
    }

    public async Task<VetVisitDetail> UpdateVisitAsync(int id, VetVisitInput input, CancellationToken cancellationToken)
    {
        var visit = await db.VetVisits.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La visita no existe.");
        var previousDate = visit.VisitDate;
        ApplyVisit(visit, input);

        if (visit.VisitDate != previousDate && await db.HealthEvents.AnyAsync(item => item.VetVisitId == id, cancellationToken))
        {
            throw ApiException.Conflict("La visita ya tiene eventos registrados: no se puede cambiar su fecha.");
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetVisitAsync(visit.Id, cancellationToken);
    }

    private static void ApplyVisit(VetVisit visit, VetVisitInput input)
    {
        var date = input.VisitDate ?? throw ApiException.BadRequest("Indica la fecha de la visita.");
        var vetName = TextInput.Clean(input.VetName) ?? throw ApiException.BadRequest("Escribe el nombre del veterinario.");
        var notes = TextInput.Clean(input.Notes);
        if (vetName.Length > 120)
        {
            throw ApiException.BadRequest("El nombre del veterinario admite hasta 120 caracteres.");
        }

        if (notes?.Length > 255)
        {
            throw ApiException.BadRequest("Las notas admiten hasta 255 caracteres.");
        }

        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de la visita no puede ser futura.");
        }

        var serviceFee = input.ServiceFee ?? 0;
        var medicationCost = input.MedicationCost ?? 0;
        foreach (var amount in new[] { serviceFee, medicationCost })
        {
            if (amount < 0 || amount > MaxAmount || !TextInput.HasTwoDecimalsAtMost(amount))
            {
                throw ApiException.Unprocessable("Los importes van de 0 a 1,000,000 con dos decimales como máximo.");
            }
        }

        visit.VisitDate = date;
        visit.VetName = vetName;
        visit.ServiceFee = serviceFee;
        visit.MedicationCost = medicationCost;
        visit.Notes = notes;
    }

    // ------------------------------------------------------------------ Eventos sanitarios

    public async Task<HealthEventPage> ListEventsAsync(HealthEventQuery query, CancellationToken cancellationToken)
    {
        EnsureRange(query.From, query.To);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var events = EventsQuery();

        if (query.AnimalId is { } animalId)
        {
            events = events.Where(item => item.AnimalId == animalId);
        }

        if (query.LotId is { } lotId)
        {
            events = events.Where(item => item.Animal.LotId == lotId);
        }

        if (query.VisitId is { } visitId)
        {
            events = events.Where(item => item.VetVisitId == visitId);
        }

        if (TextInput.Clean(query.Type) is { } type)
        {
            if (!HealthEventTypes.All.Contains(type))
            {
                throw ApiException.BadRequest("El tipo de evento no es válido.");
            }

            events = events.Where(item => item.EventType == type);
        }

        if (query.From is { } from)
        {
            events = events.Where(item => item.EventDate >= from);
        }

        if (query.To is { } to)
        {
            events = events.Where(item => item.EventDate <= to);
        }

        var total = await events.CountAsync(cancellationToken);
        var items = await LoadEventsAsync(events.Skip((page - 1) * pageSize).Take(pageSize), cancellationToken);
        return new HealthEventPage(items, total, page, pageSize);
    }

    public async Task<CreatedEvents> CreateEventsAsync(CreateEventsRequest request, CancellationToken cancellationToken)
    {
        var eventDate = request.EventDate ?? throw ApiException.BadRequest("Indica la fecha del evento.");
        var eventType = TextInput.Clean(request.EventType);
        if (eventType is null || !HealthEventTypes.All.Contains(eventType))
        {
            throw ApiException.BadRequest("Elige el tipo de evento.");
        }

        var diagnosis = TextInput.Clean(request.Diagnosis);
        if (diagnosis?.Length > 255)
        {
            throw ApiException.BadRequest("El diagnóstico admite hasta 255 caracteres.");
        }

        if (eventDate > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha del evento no puede ser futura.");
        }

        var animals = await ResolveTargetsAsync(request, cancellationToken);

        if (request.VetVisitId is { } visitId)
        {
            var visitDate = await db.VetVisits.Where(visit => visit.Id == visitId)
                .Select(visit => (DateOnly?)visit.VisitDate)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw ApiException.NotFound("La visita no existe.");
            if (visitDate != eventDate)
            {
                throw ApiException.Unprocessable($"La fecha del evento debe ser la de la visita ({visitDate:dd/MM/yyyy}).");
            }
        }

        var bornAfter = animals.Where(animal => animal.BirthDate > eventDate).Select(animal => animal.VisualTag).ToList();
        if (bornAfter.Count > 0)
        {
            throw ApiException.Unprocessable($"La fecha es anterior al nacimiento de: {Summarize(bornAfter)}.");
        }

        var treatments = await BuildTreatmentsAsync(request.Treatments ?? [], cancellationToken);
        if (treatments.Count == 0 && eventType is HealthEventTypes.Vaccination or HealthEventTypes.Deworming)
        {
            throw ApiException.Unprocessable("Una vacunación o desparasitación necesita al menos un medicamento.");
        }

        var animalIds = animals.Select(animal => animal.Id).ToList();
        var duplicated = await db.HealthEvents.AsNoTracking()
            .Where(item => animalIds.Contains(item.AnimalId) && item.EventDate == eventDate && item.EventType == eventType)
            .Select(item => item.Animal.VisualTag)
            .ToListAsync(cancellationToken);
        if (duplicated.Count > 0)
        {
            throw ApiException.Conflict($"Ya hay un evento igual ese día para: {Summarize(duplicated)}.");
        }

        var created = animals.Select(animal => new HealthEvent
        {
            AnimalId = animal.Id,
            EventDate = eventDate,
            EventType = eventType,
            Diagnosis = diagnosis,
            VetVisitId = request.VetVisitId,
            Treatments = treatments.Select(treatment => new Treatment
            {
                SupplyId = treatment.SupplyId,
                Dose = treatment.Dose,
                MilkWithdrawalDays = treatment.MilkDays,
                MeatWithdrawalDays = treatment.MeatDays,
            }).ToList(),
        }).ToList();

        db.HealthEvents.AddRange(created);
        await db.SaveChangesAsync(cancellationToken);

        var ids = created.Select(item => item.Id).ToList();
        var items = await LoadEventsAsync(EventsQuery().Where(item => ids.Contains(item.Id)), cancellationToken);
        return new CreatedEvents(items.Count, items);
    }

    public async Task DeleteEventAsync(int id, CancellationToken cancellationToken)
    {
        var item = await db.HealthEvents.Include(healthEvent => healthEvent.Treatments)
            .FirstOrDefaultAsync(healthEvent => healthEvent.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El evento no existe.");

        var now = DateTime.UtcNow;
        item.DeletedAt = now;
        item.Status = 0;
        foreach (var treatment in item.Treatments)
        {
            treatment.DeletedAt = now;
            treatment.Status = 0;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record TargetAnimal(int Id, string VisualTag, DateOnly? BirthDate);

    private sealed record PreparedTreatment(int SupplyId, string Dose, short MilkDays, short MeatDays);

    private async Task<List<TargetAnimal>> ResolveTargetsAsync(CreateEventsRequest request, CancellationToken cancellationToken)
    {
        var hasAnimals = request.AnimalIds is { Count: > 0 };
        if (hasAnimals == request.LotId.HasValue)
        {
            throw ApiException.BadRequest("Elige los animales o un lote, no ambos.");
        }

        if (request.LotId is { } lotId)
        {
            if (!await db.AnimalLots.AnyAsync(lot => lot.Id == lotId, cancellationToken))
            {
                throw ApiException.NotFound("El lote no existe.");
            }

            var inLot = await db.Animals.AsNoTracking()
                .Where(animal => animal.LotId == lotId && animal.LifecycleStatus == AnimalLifecycle.Active)
                .OrderBy(animal => animal.VisualTag)
                .Select(animal => new TargetAnimal(animal.Id, animal.VisualTag, animal.BirthDate))
                .ToListAsync(cancellationToken);
            if (inLot.Count == 0)
            {
                throw ApiException.Unprocessable("El lote no tiene animales activos.");
            }

            if (inLot.Count > MaxAnimalsPerRecord)
            {
                throw ApiException.BadRequest($"Un registro admite hasta {MaxAnimalsPerRecord} animales.");
            }

            return inLot;
        }

        var ids = request.AnimalIds!.Distinct().ToList();
        if (ids.Count > MaxAnimalsPerRecord)
        {
            throw ApiException.BadRequest($"Un registro admite hasta {MaxAnimalsPerRecord} animales.");
        }

        var found = await db.Animals.AsNoTracking()
            .Where(animal => ids.Contains(animal.Id))
            .Select(animal => new { animal.Id, animal.VisualTag, animal.BirthDate, animal.LifecycleStatus })
            .ToListAsync(cancellationToken);
        if (found.Count != ids.Count)
        {
            throw ApiException.NotFound("Uno de los animales no existe.");
        }

        var inactive = found.Where(animal => animal.LifecycleStatus != AnimalLifecycle.Active).Select(animal => animal.VisualTag).ToList();
        if (inactive.Count > 0)
        {
            throw ApiException.Conflict($"Ya no están activos en el hato: {Summarize(inactive)}.");
        }

        return found.OrderBy(animal => animal.VisualTag)
            .Select(animal => new TargetAnimal(animal.Id, animal.VisualTag, animal.BirthDate))
            .ToList();
    }

    private async Task<List<PreparedTreatment>> BuildTreatmentsAsync(IReadOnlyList<TreatmentInput> inputs, CancellationToken cancellationToken)
    {
        if (inputs.Count > MaxTreatments)
        {
            throw ApiException.BadRequest($"Un evento admite hasta {MaxTreatments} medicamentos.");
        }

        var supplyIds = inputs.Select(input => input.SupplyId).Distinct().ToList();
        var medications = await MedicationsQuery()
            .Where(supply => supplyIds.Contains(supply.Id))
            .ToDictionaryAsync(supply => supply.Id, cancellationToken);

        var prepared = new List<PreparedTreatment>();
        foreach (var input in inputs)
        {
            if (!medications.TryGetValue(input.SupplyId, out var medication))
            {
                throw ApiException.Unprocessable("Elige un medicamento del catálogo de sanidad.");
            }

            var dose = TextInput.Clean(input.Dose) ?? throw ApiException.BadRequest($"Escribe la dosis de {medication.CommercialName}.");
            if (dose.Length > 60)
            {
                throw ApiException.BadRequest("La dosis admite hasta 60 caracteres.");
            }

            var milkDays = input.MilkWithdrawalDays ?? medication.DefaultMilkWithdrawalDays;
            var meatDays = input.MeatWithdrawalDays ?? medication.DefaultMeatWithdrawalDays;
            EnsureWithdrawalDays(milkDays, meatDays);
            prepared.Add(new PreparedTreatment(medication.Id, dose, milkDays, meatDays));
        }

        return prepared;
    }

    private IQueryable<HealthEvent> EventsQuery() =>
        db.HealthEvents.AsNoTracking()
            .OrderByDescending(item => item.EventDate)
            .ThenByDescending(item => item.Id);

    private static async Task<List<HealthEventItem>> LoadEventsAsync(IQueryable<HealthEvent> query, CancellationToken cancellationToken)
    {
        var rows = await query
            .Include(item => item.Animal)
            .Include(item => item.VetVisit)
            .Include(item => item.Treatments).ThenInclude(treatment => treatment.Supply)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return rows.Select(ToEventItem).ToList();
    }

    private static HealthEventItem ToEventItem(HealthEvent item)
    {
        var treatments = item.Treatments
            .OrderBy(treatment => treatment.Id)
            .Select(treatment => new TreatmentItem(
                treatment.Id,
                treatment.SupplyId,
                treatment.Supply.CommercialName,
                treatment.Dose,
                treatment.MilkWithdrawalDays,
                treatment.MeatWithdrawalDays))
            .ToList();

        return new HealthEventItem(
            item.Id,
            item.AnimalId,
            item.Animal.VisualTag,
            item.Animal.SpeciesId,
            item.EventDate,
            item.EventType,
            item.Diagnosis,
            item.VetVisitId,
            item.VetVisit?.VetName,
            treatments,
            AllowedFrom(item.EventDate, treatments.Select(treatment => treatment.MilkWithdrawalDays)),
            AllowedFrom(item.EventDate, treatments.Select(treatment => treatment.MeatWithdrawalDays)));
    }

    /// <summary>Primer día permitido: fecha del evento más el mayor retiro; nulo si no hay retiro.</summary>
    private static DateOnly? AllowedFrom(DateOnly eventDate, IEnumerable<short> days)
    {
        var max = days.DefaultIfEmpty((short)0).Max();
        return max > 0 ? eventDate.AddDays(max) : null;
    }

    // ------------------------------------------------------------------ Retiros vigentes

    public async Task<IReadOnlyList<ActiveWithdrawalItem>> ListWithdrawalsAsync(CancellationToken cancellationToken)
    {
        var rows = await db.ActiveWithdrawals.AsNoTracking()
            .OrderBy(row => row.VisualTag)
            .ToListAsync(cancellationToken);

        var animalIds = rows.Select(row => row.AnimalId).ToList();
        var lots = await db.Animals.AsNoTracking()
            .Where(animal => animalIds.Contains(animal.Id) && animal.Lot != null)
            .Select(animal => new { animal.Id, animal.Lot!.Code })
            .ToDictionaryAsync(animal => animal.Id, animal => animal.Code, cancellationToken);

        return rows.Select(row => new ActiveWithdrawalItem(
                row.AnimalId,
                row.VisualTag,
                row.SpeciesId,
                lots.GetValueOrDefault(row.AnimalId),
                row.MilkAllowedFrom,
                row.MeatAllowedFrom))
            .ToList();
    }

    // ------------------------------------------------------------------ Laboratorio

    public async Task<IReadOnlyList<LabTestItem>> ListLabTestsAsync(int? animalId, string? result, CancellationToken cancellationToken)
    {
        var tests = db.LabTests.AsNoTracking();
        if (animalId is { } id)
        {
            tests = tests.Where(test => test.AnimalId == id);
        }

        if (TextInput.Clean(result) is { } wanted)
        {
            if (!LabResults.All.Contains(wanted))
            {
                throw ApiException.BadRequest("El resultado debe ser pending, negative, positive o suspect.");
            }

            tests = tests.Where(test => test.Result == wanted);
        }

        return await tests
            .OrderByDescending(test => test.SampleDate).ThenByDescending(test => test.Id)
            .Select(test => new LabTestItem(test.Id, test.AnimalId, test.Animal.VisualTag, test.SampleDate, test.TestType, test.LabName, test.Result))
            .ToListAsync(cancellationToken);
    }

    public async Task<LabTestItem> CreateLabTestAsync(CreateLabTestRequest request, CancellationToken cancellationToken)
    {
        var sampleDate = request.SampleDate ?? throw ApiException.BadRequest("Indica la fecha de la muestra.");
        var testType = TextInput.Clean(request.TestType) ?? throw ApiException.BadRequest("Escribe el tipo de prueba.");
        var labName = TextInput.Clean(request.LabName);
        if (testType.Length > 40)
        {
            throw ApiException.BadRequest("El tipo de prueba admite hasta 40 caracteres.");
        }

        if (labName?.Length > 120)
        {
            throw ApiException.BadRequest("El laboratorio admite hasta 120 caracteres.");
        }

        var animal = await db.Animals.AsNoTracking()
            .Where(item => item.Id == request.AnimalId)
            .Select(item => new { item.Id, item.BirthDate })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw ApiException.NotFound("El animal no existe.");

        if (sampleDate > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de la muestra no puede ser futura.");
        }

        if (animal.BirthDate is { } birth && sampleDate < birth)
        {
            throw ApiException.Unprocessable("La fecha de la muestra no puede ser anterior al nacimiento.");
        }

        var test = new LabTest { AnimalId = animal.Id, SampleDate = sampleDate, TestType = testType, LabName = labName };
        db.LabTests.Add(test);
        await db.SaveChangesAsync(cancellationToken);
        return await GetLabTestAsync(test.Id, cancellationToken);
    }

    public async Task<LabTestItem> SetLabResultAsync(int id, LabTestResultRequest request, CancellationToken cancellationToken)
    {
        var result = TextInput.Clean(request.Result);
        if (result is null || !LabResults.All.Contains(result))
        {
            throw ApiException.BadRequest("El resultado debe ser pendiente, negativo, positivo o sospechoso.");
        }

        var test = await db.LabTests.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La prueba no existe.");
        test.Result = result;
        await db.SaveChangesAsync(cancellationToken);
        return await GetLabTestAsync(test.Id, cancellationToken);
    }

    private async Task<LabTestItem> GetLabTestAsync(int id, CancellationToken cancellationToken) =>
        await db.LabTests.AsNoTracking()
            .Where(test => test.Id == id)
            .Select(test => new LabTestItem(test.Id, test.AnimalId, test.Animal.VisualTag, test.SampleDate, test.TestType, test.LabName, test.Result))
            .FirstAsync(cancellationToken);

    // ------------------------------------------------------------------ Utilidades

    private static void EnsureWithdrawalDays(short milkDays, short meatDays)
    {
        if (milkDays is < 0 or > MaxWithdrawalDays || meatDays is < 0 or > MaxWithdrawalDays)
        {
            throw ApiException.BadRequest($"Los días de retiro van de 0 a {MaxWithdrawalDays}.");
        }
    }

    private static void EnsureRange(DateOnly? from, DateOnly? to)
    {
        if (from > to)
        {
            throw ApiException.BadRequest("La fecha inicial no puede ser posterior a la final.");
        }
    }

    /// <summary>Lista corta de aretes para mensajes: los primeros cinco y cuántos más.</summary>
    private static string Summarize(IReadOnlyCollection<string> tags)
    {
        var shown = string.Join(", ", tags.Take(5));
        return tags.Count > 5 ? $"{shown} y {tags.Count - 5} más" : shown;
    }
}
