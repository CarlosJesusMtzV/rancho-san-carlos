using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Milking;

public sealed class MilkingService(TecNMDbContext db)
{
    private const decimal MaxLiters = 2000m;
    private const int DefaultRangeDays = 60;

    // ------------------------------------------------------------------ Cabras

    public async Task<IReadOnlyList<MilkingGoat>> ListGoatsAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var day = date ?? RanchClock.Today;
        var goats = await ActiveFemaleGoats()
            .OrderBy(animal => animal.VisualTag)
            .Select(animal => new
            {
                animal.Id,
                animal.VisualTag,
                animal.ProductionStage,
                LotCode = animal.Lot != null ? animal.Lot.Code : null,
            })
            .ToListAsync(cancellationToken);

        var withdrawals = await MilkWithdrawalsAsync(goats.Select(goat => goat.Id).ToList(), day, cancellationToken);
        return goats.Select(goat =>
            {
                DateOnly? allowedFrom = withdrawals.TryGetValue(goat.Id, out var until) ? until : null;
                return new MilkingGoat(goat.Id, goat.VisualTag, goat.ProductionStage, goat.LotCode, allowedFrom is null, allowedFrom);
            })
            .ToList();
    }

    // ------------------------------------------------------------------ Ordeñas

    public async Task<IReadOnlyList<MilkDaySummary>> ListDaysAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var end = to ?? RanchClock.Today;
        var start = from ?? end.AddDays(-DefaultRangeDays);
        if (start > end)
        {
            throw ApiException.BadRequest("La fecha inicial no puede ser posterior a la final.");
        }

        return await SummaryQuery(db.MilkLots.Where(lot => lot.CollectionDate >= start && lot.CollectionDate <= end))
            .ToListAsync(cancellationToken);
    }

    public async Task<MilkDayDetail> GetDayAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await db.MilkLots.AsNoTracking()
            .Include(item => item.Records).ThenInclude(record => record.Animal)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La ordeña no existe.");

        var goats = lot.Records
            .OrderBy(record => record.Animal.VisualTag)
            .Select(record => new MilkedGoat(record.AnimalId, record.Animal.VisualTag))
            .ToList();
        var batch = await CheeseBatchAsync(lot.Id, cancellationToken);
        return new MilkDayDetail(lot.Id, lot.Code, lot.CollectionDate, lot.TotalLiters, goats.Count, PerGoat(lot.TotalLiters, goats.Count), lot.Notes, goats, batch);
    }

    public async Task<MilkDayDetail> CreateDayAsync(CreateMilkDayRequest request, CancellationToken cancellationToken)
    {
        var date = request.CollectionDate ?? throw ApiException.BadRequest("Indica la fecha de la ordeña.");
        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de la ordeña no puede ser futura.");
        }

        var (liters, goatIds, notes) = ValidateFields(request.TotalLiters, request.GoatIds, request.Notes);
        if (await db.MilkLots.AnyAsync(lot => lot.CollectionDate == date, cancellationToken))
        {
            throw ApiException.Conflict($"Ya hay una ordeña registrada el {date:dd/MM/yyyy}. Edítala si hace falta corregirla.");
        }

        await EnsureGoatsCanBeMilkedAsync(goatIds, date, cancellationToken);

        var lot = new MilkLot
        {
            Code = await NextCodeAsync(date, cancellationToken),
            CollectionDate = date,
            TotalLiters = liters,
            Notes = notes,
            Records = goatIds.Select(goatId => new MilkRecord { AnimalId = goatId }).ToList(),
        };
        db.MilkLots.Add(lot);
        await db.SaveChangesAsync(cancellationToken);
        return await GetDayAsync(lot.Id, cancellationToken);
    }

    public async Task<MilkDayDetail> UpdateDayAsync(int id, UpdateMilkDayRequest request, CancellationToken cancellationToken)
    {
        var (liters, goatIds, notes) = ValidateFields(request.TotalLiters, request.GoatIds, request.Notes);
        var lot = await db.MilkLots.Include(item => item.Records)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La ordeña no existe.");

        var batch = await CheeseBatchAsync(lot.Id, cancellationToken);
        if (batch is not null && liters < batch.LitersUsed)
        {
            throw ApiException.Unprocessable($"La tanda {batch.Code} usó {batch.LitersUsed:0.##} L de esta ordeña; el total no puede ser menor.");
        }

        var current = lot.Records.Select(record => record.AnimalId).ToHashSet();
        var added = goatIds.Where(goatId => !current.Contains(goatId)).ToList();
        await EnsureGoatsCanBeMilkedAsync(added, lot.CollectionDate, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var record in lot.Records.Where(record => !goatIds.Contains(record.AnimalId)))
        {
            record.DeletedAt = now;
            record.Status = 0;
        }

        lot.Records.AddRange(added.Select(goatId => new MilkRecord { AnimalId = goatId }));
        lot.TotalLiters = liters;
        lot.Notes = notes;
        await db.SaveChangesAsync(cancellationToken);
        return await GetDayAsync(lot.Id, cancellationToken);
    }

    public async Task DeleteDayAsync(int id, CancellationToken cancellationToken)
    {
        var lot = await db.MilkLots.Include(item => item.Records)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La ordeña no existe.");

        var batch = await CheeseBatchAsync(lot.Id, cancellationToken);
        if (batch is not null)
        {
            throw ApiException.Conflict($"La leche de esta ordeña se usó en la tanda {batch.Code}; elimina la tanda primero en Quesería.");
        }

        var now = DateTime.UtcNow;
        lot.DeletedAt = now;
        lot.Status = 0;
        foreach (var record in lot.Records)
        {
            record.DeletedAt = now;
            record.Status = 0;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ Resumen

    public async Task<MilkSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var today = RanchClock.Today;
        var days = await SummaryQuery(db.MilkLots.Where(lot => lot.CollectionDate > today.AddDays(-60) && lot.CollectionDate <= today))
            .ToListAsync(cancellationToken);

        MilkPeriod Period(int fromDaysAgo, int toDaysAgo)
        {
            var inPeriod = days
                .Where(day => day.CollectionDate > today.AddDays(-fromDaysAgo) && day.CollectionDate <= today.AddDays(-toDaysAgo))
                .ToList();
            var liters = inPeriod.Sum(day => day.TotalLiters);
            return new MilkPeriod(inPeriod.Count, liters, inPeriod.Count == 0 ? 0 : decimal.Round(liters / inPeriod.Count, 2));
        }

        var lastDay = days.FirstOrDefault() ?? await SummaryQuery(db.MilkLots).FirstOrDefaultAsync(cancellationToken);
        return new MilkSummary(lastDay, Period(7, 0), Period(30, 0), Period(60, 30));
    }

    // ------------------------------------------------------------------ Utilidades

    private IQueryable<Animal> ActiveFemaleGoats() =>
        db.Animals.AsNoTracking().Where(animal =>
            animal.SpeciesId == Species.GoatId && animal.Sex == "F" && animal.LifecycleStatus == AnimalLifecycle.Active);

    private static IQueryable<MilkDaySummary> SummaryQuery(IQueryable<MilkLot> lots) =>
        lots.AsNoTracking()
            .OrderByDescending(lot => lot.CollectionDate)
            .Select(lot => new MilkDaySummary(
                lot.Id,
                lot.Code,
                lot.CollectionDate,
                lot.TotalLiters,
                lot.Records.Count(),
                lot.Records.Any() ? Math.Round(lot.TotalLiters / lot.Records.Count(), 2) : 0,
                lot.Notes));

    private static (decimal Liters, List<int> GoatIds, string? Notes) ValidateFields(decimal? totalLiters, IReadOnlyList<int>? goatIds, string? notes)
    {
        var liters = totalLiters ?? throw ApiException.BadRequest("Indica el total de litros.");
        if (liters <= 0 || liters > MaxLiters || !TextInput.HasTwoDecimalsAtMost(liters))
        {
            throw ApiException.Unprocessable($"El total va de 0.01 a {MaxLiters:0} litros con dos decimales como máximo.");
        }

        var ids = goatIds ?? [];
        if (ids.Count == 0)
        {
            throw ApiException.BadRequest("Elige al menos una cabra ordeñada.");
        }

        if (ids.Distinct().Count() != ids.Count)
        {
            throw ApiException.BadRequest("Una cabra aparece dos veces en la ordeña.");
        }

        var cleanNotes = TextInput.Clean(notes);
        if (cleanNotes?.Length > 255)
        {
            throw ApiException.BadRequest("Las notas admiten hasta 255 caracteres.");
        }

        return (liters, ids.ToList(), cleanNotes);
    }

    private async Task EnsureGoatsCanBeMilkedAsync(IReadOnlyList<int> goatIds, DateOnly date, CancellationToken cancellationToken)
    {
        if (goatIds.Count == 0)
        {
            return;
        }

        var goats = await ActiveFemaleGoats()
            .Where(animal => goatIds.Contains(animal.Id))
            .Select(animal => new { animal.Id, animal.VisualTag })
            .ToListAsync(cancellationToken);
        if (goats.Count != goatIds.Count)
        {
            throw ApiException.Unprocessable("Solo se ordeñan cabras hembra activas: revisa la lista.");
        }

        var withdrawals = await MilkWithdrawalsAsync(goatIds, date, cancellationToken);
        var blocked = goats
            .Where(goat => withdrawals.ContainsKey(goat.Id))
            .Select(goat => $"{goat.VisualTag} (hasta el {withdrawals[goat.Id]:dd/MM/yyyy})")
            .ToList();
        if (blocked.Count > 0)
        {
            throw ApiException.Unprocessable($"Con retiro de leche: {string.Join(", ", blocked)}.");
        }
    }

    /// <summary>
    /// Primer día en que la leche de cada cabra puede usarse, si en la fecha indicada sigue en retiro.
    /// Misma regla que fn_withdrawal_allowed_from en la base de datos.
    /// </summary>
    private async Task<Dictionary<int, DateOnly>> MilkWithdrawalsAsync(IReadOnlyCollection<int> goatIds, DateOnly date, CancellationToken cancellationToken)
    {
        var treatments = await db.Treatments.AsNoTracking()
            .Where(treatment => treatment.MilkWithdrawalDays > 0
                && goatIds.Contains(treatment.HealthEvent.AnimalId)
                && treatment.HealthEvent.EventDate <= date)
            .Select(treatment => new { treatment.HealthEvent.AnimalId, treatment.HealthEvent.EventDate, treatment.MilkWithdrawalDays })
            .ToListAsync(cancellationToken);

        return treatments
            .Select(treatment => new { treatment.AnimalId, AllowedFrom = treatment.EventDate.AddDays(treatment.MilkWithdrawalDays) })
            .Where(item => item.AllowedFrom > date)
            .GroupBy(item => item.AnimalId)
            .ToDictionary(group => group.Key, group => group.Max(item => item.AllowedFrom));
    }

    private async Task<string> NextCodeAsync(DateOnly date, CancellationToken cancellationToken)
    {
        // El código es único aun entre ordeñas borradas; si se volvió a registrar el día, lleva sufijo.
        var code = $"LL-{date:yyyyMMdd}";
        var taken = await db.MilkLots.IgnoreQueryFilters()
            .Where(lot => lot.Code == code || lot.Code.StartsWith(code + "-"))
            .Select(lot => lot.Code)
            .ToListAsync(cancellationToken);
        if (!taken.Contains(code))
        {
            return code;
        }

        var suffix = 2;
        while (taken.Contains($"{code}-{suffix}"))
        {
            suffix++;
        }

        return $"{code}-{suffix}";
    }

    private Task<MilkDayCheeseBatch?> CheeseBatchAsync(int milkLotId, CancellationToken cancellationToken) =>
        db.CheeseBatches.AsNoTracking()
            .Where(batch => batch.MilkLotId == milkLotId)
            .Select(batch => new MilkDayCheeseBatch(batch.Id, batch.Code, batch.LitersUsed))
            .FirstOrDefaultAsync(cancellationToken);

    private static decimal PerGoat(decimal liters, int goats) => goats == 0 ? 0 : decimal.Round(liters / goats, 2);
}
