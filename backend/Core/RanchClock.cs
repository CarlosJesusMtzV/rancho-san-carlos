namespace TecNM.Api.Core;

/// <summary>Fecha local del rancho (Castaños, Coahuila: UTC−6 todo el año).</summary>
public static class RanchClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone));

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Monterrey");
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Imágenes sin tzdata: Coahuila no usa horario de verano desde 2022.
            return TimeZoneInfo.CreateCustomTimeZone("Rancho", TimeSpan.FromHours(-6), "Rancho", "Rancho");
        }
    }
}
