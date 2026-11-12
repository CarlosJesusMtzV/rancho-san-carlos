using Microsoft.AspNetCore.Http;

namespace TecNM.Api.Core.Errors;

/// <summary>
/// Error esperado de negocio o de entrada. El mensaje se muestra al usuario, por lo que va en español.
/// </summary>
public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ApiException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);

    public static ApiException NotFound(string message) => new(StatusCodes.Status404NotFound, message);

    public static ApiException Conflict(string message) => new(StatusCodes.Status409Conflict, message);

    public static ApiException Unprocessable(string message) => new(StatusCodes.Status422UnprocessableEntity, message);
}
