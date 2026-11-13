using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Slaughter;

public sealed class SlaughterService(TecNMDbContext db)
{
    private const int DefaultRangeDays = 180;
    private const decimal MaxWeightKg = 100m;
    private const decimal MaxCost = 1_000_000m;

    // ------------------------------------------------------------------ Cabritos

    public async Task<IReadOnlyList<SlaughterCandidate>> ListKidsAsync(DateOnly? date, CancellationToken cancellationToken)
    {
        var day = date ?? RanchClock.Today;
        var goats = await db.Animals.AsNoTracking()
            .Where(animal => animal.SpeciesId == Species.GoatId && animal.LifecycleStatus == AnimalLifecycle.Active)
            .Select(animal => new
            {
                animal.Id,
                animal.VisualTag,
                animal.Sex,
                animal.BirthDate,
                animal.ProductionStage,
                LastWeight = animal.Weights.OrderByDescending(weight => weight.WeighedOn).Select(weight => (decimal?)weight.WeightKg).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        var withdrawals = await MeatWithdrawalsAsync(goats.Select(goat => goat.Id).ToList(), day, cancellationToken);

        return goats
            .OrderBy(goat => goat.BirthDate is null).ThenByDescending(goat => goat.BirthDate).ThenBy(goat => goat.VisualTag)
            .Select(goat =>
            {
                DateOnly? allowedFrom = withdrawals.TryGetValue(goat.Id, out var until) ? until : null;
                return new SlaughterCandidate(goat.Id, goat.VisualTag, goat.Sex, AnimalAge.InMonths(goat.BirthDate, day), goat.ProductionStage, goat.LastWeight, allowedFrom is null, allowedFrom);
            })
            .ToList();
    }

    // ------------------------------------------------------------------ Jornadas

    public async Task<IReadOnlyList<SlaughterRunSummary>> ListRunsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var end = to ?? RanchClock.Today;
        var start = from ?? end.AddDays(-DefaultRangeDays);
        if (start > end)
        {
            throw ApiException.BadRequest("La fecha inicial no puede ser posterior a la final.");
        }

        var runs = await db.SlaughterRuns.AsNoTracking().Include(run => run.Kids)
            .Where(run => run.RunDate >= start && run.RunDate <= end)
            .OrderByDescending(run => run.RunDate).ThenByDescending(run => run.Id)
            .ToListAsync(cancellationToken);
        return runs.Select(ToSummary).ToList();
    }

    public async Task<SlaughterRunDetail> GetRunAsync(int id, CancellationToken cancellationToken)
    {
        var run = await db.SlaughterRuns.AsNoTracking()
            .Include(item => item.Kids).ThenInclude(kid => kid.Animal)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La jornada de matanza no existe.");
        var sold = await SoldAsync(run.Kids.Select(kid => kid.Id).ToList(), cancellationToken);
        var summary = ToSummary(run);
        var kids = run.Kids
            .OrderBy(kid => kid.Animal.VisualTag)
            .Select(kid => new SlaughteredKid(
                kid.Id,
                kid.AnimalId,
                kid.Animal.VisualTag,
                kid.Animal.Sex,
                AnimalAge.InMonths(kid.Animal.BirthDate, run.RunDate),
                kid.LiveWeightKg,
                kid.CarcassWeightKg,
                YieldPercent(kid.CarcassWeightKg, kid.LiveWeightKg),
                sold.Contains(kid.Id)))
            .ToList();
        return new SlaughterRunDetail(summary.Id, summary.RunDate, summary.Slaughterhouse, summary.Cost, summary.KidCount, summary.LiveWeightKg,
            summary.CarcassWeightKg, summary.YieldPercent, summary.CostPerKid, summary.Notes, kids);
    }

    public async Task<SlaughterRunDetail> CreateRunAsync(SaveRunRequest request, CancellationToken cancellationToken)
    {
        var date = request.RunDate ?? throw ApiException.BadRequest("Indica la fecha de la matanza.");
        if (date > RanchClock.Today)
        {
            throw ApiException.Unprocessable("La fecha de la matanza no puede ser futura.");
        }

        var header = ValidateHeader(request);
        var kids = ValidateKids(request.Kids);
        var animals = await LoadNewKidsAsync(kids.Select(kid => kid.AnimalId).ToList(), date, cancellationToken);

        var run = new SlaughterRun
        {
            RunDate = date,
            Slaughterhouse = header.Slaughterhouse,
            Cost = header.Cost,
            Notes = header.Notes,
            Kids = kids.Select(kid => new KidSlaughter { AnimalId = kid.AnimalId, LiveWeightKg = kid.Live, CarcassWeightKg = kid.Carcass }).ToList(),
        };
        foreach (var animal in animals)
        {
            animal.LifecycleStatus = AnimalLifecycle.Slaughtered;
            animal.ExitDate = date;
        }

        db.SlaughterRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        return await GetRunAsync(run.Id, cancellationToken);
    }

    public async Task<SlaughterRunDetail> UpdateRunAsync(int id, SaveRunRequest request, CancellationToken cancellationToken)
    {
        var run = await db.SlaughterRuns.Include(item => item.Kids).ThenInclude(kid => kid.Animal)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La jornada de matanza no existe.");
        if (request.RunDate is { } date && date != run.RunDate)
        {
            throw ApiException.Unprocessable("La fecha de una jornada no se cambia; elimínala y regístrala de nuevo.");
        }

        var header = ValidateHeader(request);
        var kids = ValidateKids(request.Kids);
        var wanted = kids.ToDictionary(kid => kid.AnimalId);
        var removed = run.Kids.Where(kid => !wanted.ContainsKey(kid.AnimalId)).ToList();
        var sold = await SoldAsync(removed.Select(kid => kid.Id).ToList(), cancellationToken);
        var blocked = removed.FirstOrDefault(kid => sold.Contains(kid.Id));
        if (blocked is not null)
        {
            throw ApiException.Unprocessable($"La canal de {blocked.Animal.VisualTag} ya está en un pedido; no se puede quitar de la jornada.");
        }

        var current = run.Kids.Select(kid => kid.AnimalId).ToHashSet();
        var added = kids.Where(kid => !current.Contains(kid.AnimalId)).ToList();
        var newAnimals = await LoadNewKidsAsync(added.Select(kid => kid.AnimalId).ToList(), run.RunDate, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var kid in removed)
        {
            kid.DeletedAt = now;
            kid.Status = 0;
            Restore(kid.Animal);
        }

        foreach (var kid in run.Kids.Where(kid => wanted.ContainsKey(kid.AnimalId)))
        {
            kid.LiveWeightKg = wanted[kid.AnimalId].Live;
            kid.CarcassWeightKg = wanted[kid.AnimalId].Carcass;
        }

        run.Kids.AddRange(added.Select(kid => new KidSlaughter { AnimalId = kid.AnimalId, LiveWeightKg = kid.Live, CarcassWeightKg = kid.Carcass }));
        foreach (var animal in newAnimals)
        {
            animal.LifecycleStatus = AnimalLifecycle.Slaughtered;
            animal.ExitDate = run.RunDate;
        }

        run.Slaughterhouse = header.Slaughterhouse;
        run.Cost = header.Cost;
        run.Notes = header.Notes;
        await db.SaveChangesAsync(cancellationToken);
        return await GetRunAsync(run.Id, cancellationToken);
    }

    public async Task DeleteRunAsync(int id, CancellationToken cancellationToken)
    {
        var run = await db.SlaughterRuns.Include(item => item.Kids).ThenInclude(kid => kid.Animal)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("La jornada de matanza no existe.");
        var sold = await SoldAsync(run.Kids.Select(kid => kid.Id).ToList(), cancellationToken);
        var blocked = run.Kids.FirstOrDefault(kid => sold.Contains(kid.Id));
        if (blocked is not null)
        {
            throw ApiException.Conflict($"La canal de {blocked.Animal.VisualTag} ya está en un pedido; no se puede eliminar la jornada.");
        }

        var now = DateTime.UtcNow;
        foreach (var kid in run.Kids)
        {
            kid.DeletedAt = now;
            kid.Status = 0;
            Restore(kid.Animal);
        }

        run.DeletedAt = now;
        run.Status = 0;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ------------------------------------------------------------------ Canales y resumen

    public async Task<IReadOnlyList<Carcass>> ListCarcassesAsync(CancellationToken cancellationToken)
    {
        var kids = await db.KidSlaughters.AsNoTracking()
            .Select(kid => new { kid.Id, kid.AnimalId, kid.Animal.VisualTag, kid.SlaughterRunId, kid.SlaughterRun.RunDate, kid.SlaughterRun.Slaughterhouse, kid.CarcassWeightKg })
            .ToListAsync(cancellationToken);
        var sold = await SoldAsync(kids.Select(kid => kid.Id).ToList(), cancellationToken);
        var today = RanchClock.Today;
        return kids
            .Where(kid => !sold.Contains(kid.Id))
            .OrderBy(kid => kid.RunDate).ThenBy(kid => kid.VisualTag)
            .Select(kid => new Carcass(kid.Id, kid.AnimalId, kid.VisualTag, kid.SlaughterRunId, kid.RunDate, kid.Slaughterhouse, kid.CarcassWeightKg, today.DayNumber - kid.RunDate.DayNumber))
            .ToList();
    }

    public async Task<SlaughterSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var carcasses = await ListCarcassesAsync(cancellationToken);
        var today = RanchClock.Today;
        var since = today.AddDays(-DefaultRangeDays);
        var recent = await db.KidSlaughters.AsNoTracking()
            .Where(kid => kid.SlaughterRun.RunDate > since)
            .Select(kid => new { kid.SlaughterRun.RunDate, kid.LiveWeightKg, kid.CarcassWeightKg })
            .ToListAsync(cancellationToken);
        var live = recent.Sum(kid => kid.LiveWeightKg);
        var lastRun = await db.SlaughterRuns.AsNoTracking().Include(run => run.Kids)
            .OrderByDescending(run => run.RunDate).ThenByDescending(run => run.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return new SlaughterSummary(
            carcasses.Count,
            carcasses.Sum(carcass => carcass.CarcassWeightKg),
            recent.Count(kid => kid.RunDate > today.AddDays(-30)),
            live == 0 ? null : YieldPercent(recent.Sum(kid => kid.CarcassWeightKg), live),
            lastRun is null ? null : ToSummary(lastRun));
    }

    // ------------------------------------------------------------------ Validación

    private sealed record Header(string Slaughterhouse, decimal? Cost, string? Notes);

    private sealed record KidWeights(int AnimalId, decimal Live, decimal Carcass);

    private static Header ValidateHeader(SaveRunRequest request)
    {
        var place = TextInput.Clean(request.Slaughterhouse) ?? throw ApiException.BadRequest("Escribe el rastro o matancero.");
        if (place.Length > 120)
        {
            throw ApiException.BadRequest("El rastro o matancero admite hasta 120 caracteres.");
        }

        if (request.Cost is { } cost && (cost < 0 || cost > MaxCost || !TextInput.HasTwoDecimalsAtMost(cost)))
        {
            throw ApiException.Unprocessable("El pago va de $0 a $1,000,000 con dos decimales como máximo.");
        }

        var notes = TextInput.Clean(request.Notes);
        if (notes?.Length > 255)
        {
            throw ApiException.BadRequest("Las notas admiten hasta 255 caracteres.");
        }

        return new Header(place, request.Cost, notes);
    }

    private static List<KidWeights> ValidateKids(IReadOnlyList<KidWeightsRequest>? kids)
    {
        var items = kids ?? [];
        if (items.Count == 0)
        {
            throw ApiException.BadRequest("Marca al menos un cabrito.");
        }

        if (items.Any(kid => kid.AnimalId is null))
        {
            throw ApiException.BadRequest("Cada cabrito necesita su animal.");
        }

        if (items.Select(kid => kid.AnimalId).Distinct().Count() != items.Count)
        {
            throw ApiException.BadRequest("Un cabrito aparece dos veces en la jornada.");
        }

        var result = new List<KidWeights>();
        foreach (var kid in items)
        {
            if (kid.LiveWeightKg is not { } live || kid.CarcassWeightKg is not { } carcass)
            {
                throw ApiException.BadRequest("Indica el peso vivo y el peso en canal de cada cabrito.");
            }

            if (live <= 0 || carcass <= 0 || live > MaxWeightKg || carcass > MaxWeightKg || !TextInput.HasTwoDecimalsAtMost(live) || !TextInput.HasTwoDecimalsAtMost(carcass))
            {
                throw ApiException.Unprocessable($"Los pesos van de 0.01 a {MaxWeightKg:0} kg con dos decimales como máximo.");
            }

            if (carcass > live)
            {
                throw ApiException.Unprocessable($"La canal ({carcass:0.##} kg) no puede pesar más que el cabrito vivo ({live:0.##} kg).");
            }

            result.Add(new KidWeights(kid.AnimalId!.Value, live, carcass));
        }

        return result;
    }

    /// <summary>Carga, con seguimiento, los cabritos que entran a la jornada y verifica que puedan sacrificarse.</summary>
    private async Task<List<Animal>> LoadNewKidsAsync(IReadOnlyList<int> animalIds, DateOnly date, CancellationToken cancellationToken)
    {
        if (animalIds.Count == 0)
        {
            return [];
        }

        var animals = await db.Animals.Where(animal => animalIds.Contains(animal.Id)).ToListAsync(cancellationToken);
        if (animals.Count != animalIds.Count)
        {
            throw ApiException.NotFound("Uno de los cabritos no existe.");
        }

        foreach (var animal in animals)
        {
            if (animal.SpeciesId != Species.GoatId)
            {
                throw ApiException.Unprocessable($"{animal.VisualTag} no es caprino: solo se sacrifican cabritos.");
            }

            if (animal.LifecycleStatus == AnimalLifecycle.Slaughtered)
            {
                throw ApiException.Conflict($"{animal.VisualTag} ya fue sacrificado.");
            }

            if (animal.LifecycleStatus != AnimalLifecycle.Active)
            {
                throw ApiException.Unprocessable($"{animal.VisualTag} ya salió del hato.");
            }

            if (animal.BirthDate is { } birth && birth > date)
            {
                throw ApiException.Unprocessable($"La matanza no puede ser anterior al nacimiento de {animal.VisualTag} ({Day(birth)}).");
            }
        }

        var withdrawals = await MeatWithdrawalsAsync(animalIds, date, cancellationToken);
        var blocked = animals
            .Where(animal => withdrawals.ContainsKey(animal.Id))
            .Select(animal => $"{animal.VisualTag} (hasta el {Day(withdrawals[animal.Id])})")
            .ToList();
        return blocked.Count > 0 ? throw ApiException.Unprocessable($"Con retiro de carne: {string.Join(", ", blocked)}.") : animals;
    }

    private static void Restore(Animal animal)
    {
        animal.LifecycleStatus = AnimalLifecycle.Active;
        animal.ExitDate = null;
    }

    /// <summary>Primer día en que cada cabrito puede sacrificarse, si en la fecha sigue en retiro. Misma regla que fn_withdrawal_allowed_from.</summary>
    private async Task<Dictionary<int, DateOnly>> MeatWithdrawalsAsync(IReadOnlyCollection<int> animalIds, DateOnly date, CancellationToken cancellationToken)
    {
        var treatments = await db.Treatments.AsNoTracking()
            .Where(treatment => treatment.MeatWithdrawalDays > 0
                && animalIds.Contains(treatment.HealthEvent.AnimalId)
                && treatment.HealthEvent.EventDate <= date)
            .Select(treatment => new { treatment.HealthEvent.AnimalId, treatment.HealthEvent.EventDate, treatment.MeatWithdrawalDays })
            .ToListAsync(cancellationToken);
        return treatments
            .Select(treatment => new { treatment.AnimalId, AllowedFrom = treatment.EventDate.AddDays(treatment.MeatWithdrawalDays) })
            .Where(item => item.AllowedFrom > date)
            .GroupBy(item => item.AnimalId)
            .ToDictionary(group => group.Key, group => group.Max(item => item.AllowedFrom));
    }

    /// <summary>Matanzas cuya canal está en un renglón de pedido vigente (no cancelado).</summary>
    private async Task<HashSet<int>> SoldAsync(IReadOnlyCollection<int> slaughterIds, CancellationToken cancellationToken)
    {
        if (slaughterIds.Count == 0)
        {
            return [];
        }

        var ids = await db.OrderItems.AsNoTracking()
            .Where(item => item.KidSlaughterId != null && slaughterIds.Contains(item.KidSlaughterId.Value)
                && db.Orders.Any(order => order.Id == item.OrderId && order.OrderStatus != Order.Cancelled))
            .Select(item => item.KidSlaughterId!.Value)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    private static SlaughterRunSummary ToSummary(SlaughterRun run)
    {
        var live = run.Kids.Sum(kid => kid.LiveWeightKg);
        var carcass = run.Kids.Sum(kid => kid.CarcassWeightKg);
        var count = run.Kids.Count;
        return new SlaughterRunSummary(
            run.Id,
            run.RunDate,
            run.Slaughterhouse,
            run.Cost,
            count,
            live,
            carcass,
            live == 0 ? 0 : YieldPercent(carcass, live),
            run.Cost is { } cost && count > 0 ? decimal.Round(cost / count, 2) : null,
            run.Notes);
    }

    private static decimal YieldPercent(decimal carcass, decimal live) => decimal.Round(carcass / live * 100, 1);

    private static string Day(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
