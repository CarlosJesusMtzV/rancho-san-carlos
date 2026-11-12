namespace TecNM.Api.Core;

/// <summary>Normalización de texto capturado por el usuario, compartida entre módulos.</summary>
public static class TextInput
{
    /// <summary>Recorta espacios y convierte la cadena vacía en nulo.</summary>
    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Aretes y códigos: sin espacios sobrantes y en mayúsculas.</summary>
    public static string? CleanCode(string? value) => Clean(value)?.ToUpperInvariant();

    /// <summary>Verdadero si el importe tiene como máximo dos decimales.</summary>
    public static bool HasTwoDecimalsAtMost(decimal value) => decimal.Round(value, 2) == value;
}
