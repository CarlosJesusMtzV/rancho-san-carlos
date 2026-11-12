using TecNM.Api.Core;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Modules.Herd;

/// <summary>Catálogos y cálculos del módulo Hato que no dependen de la base de datos.</summary>
public static class HerdRules
{
    public const decimal MinWeightKg = 0.1m;
    public const decimal MaxWeightKg = 1500m;

    private sealed record Stage(short SpeciesId, string Code, string Sexes);

    private static readonly Stage[] Stages =
    [
        new(Species.CattleId, "calf", "MF"),
        new(Species.CattleId, "steer", "M"),
        new(Species.CattleId, "heifer", "F"),
        new(Species.CattleId, "cow", "F"),
        new(Species.CattleId, "bull", "M"),
        new(Species.GoatId, "kid", "MF"),
        new(Species.GoatId, "doeling", "F"),
        new(Species.GoatId, "milking_doe", "F"),
        new(Species.GoatId, "dry_doe", "F"),
        new(Species.GoatId, "buck", "M"),
    ];

    public static readonly IReadOnlySet<string> ManualExitStatuses =
        new HashSet<string> { AnimalLifecycle.Sold, AnimalLifecycle.Dead, AnimalLifecycle.Culled };

    public static bool IsValidStage(short speciesId, string stage, string sex) =>
        Stages.Any(item => item.SpeciesId == speciesId && item.Code == stage && item.Sexes.Contains(sex, StringComparison.Ordinal));

    public static bool StageExists(short speciesId, string stage) =>
        Stages.Any(item => item.SpeciesId == speciesId && item.Code == stage);

    /// <summary>Meses cumplidos entre el nacimiento y la fecha indicada.</summary>
    public static int? AgeInMonths(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is not { } birth || birth > today)
        {
            return null;
        }

        var months = (today.Year - birth.Year) * 12 + today.Month - birth.Month;
        if (today.Day < birth.Day)
        {
            months--;
        }

        return months;
    }

    public static string? Clean(string? value) => TextInput.Clean(value);

    public static string? CleanCode(string? value) => TextInput.CleanCode(value);
}
