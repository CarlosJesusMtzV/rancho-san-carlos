namespace TecNM.Api.Core;

public static class AnimalAge
{
    /// <summary>Meses cumplidos entre el nacimiento y la fecha indicada; nulo si no hay fecha o es futura.</summary>
    public static int? InMonths(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is not { } birth || birth > today)
        {
            return null;
        }

        var months = (today.Year - birth.Year) * 12 + today.Month - birth.Month;
        return today.Day < birth.Day ? months - 1 : months;
    }
}
