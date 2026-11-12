using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace TecNM.Api.Core.Errors;

/// <summary>
/// Convierte excepciones en application/problem+json con mensajes en español.
/// Nunca devuelve el detalle técnico; ese solo se registra en el log.
/// </summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = Describe(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado en {Path}", context.Request.Path);
        }
        else
        {
            logger.LogInformation("Petición rechazada en {Path}: {Status} {Detail}", context.Request.Path, status, detail);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json", cancellationToken);
        return true;
    }

    private static (int Status, string Title, string Detail) Describe(Exception exception) => exception switch
    {
        ApiException api => (api.StatusCode, TitleFor(api.StatusCode), api.Message),
        DbUpdateException { InnerException: PostgresException postgres } => DescribePostgres(postgres),
        BadHttpRequestException => (StatusCodes.Status400BadRequest, TitleFor(400), "La petición no tiene un formato válido."),
        _ => (StatusCodes.Status500InternalServerError, TitleFor(500), "Ocurrió un error inesperado. Intenta de nuevo más tarde."),
    };

    private static (int Status, string Title, string Detail) DescribePostgres(PostgresException postgres) => postgres.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation =>
            (StatusCodes.Status409Conflict, TitleFor(409), "Ya existe un registro con esos datos."),
        // Los triggers del rancho lanzan check_violation sin restricción asociada y con el mensaje en español.
        PostgresErrorCodes.CheckViolation when string.IsNullOrEmpty(postgres.ConstraintName) =>
            (StatusCodes.Status422UnprocessableEntity, TitleFor(422), postgres.MessageText),
        PostgresErrorCodes.CheckViolation =>
            (StatusCodes.Status422UnprocessableEntity, TitleFor(422), "Los datos no cumplen una regla del sistema."),
        PostgresErrorCodes.ForeignKeyViolation =>
            (StatusCodes.Status422UnprocessableEntity, TitleFor(422), "Un registro relacionado no existe o no corresponde."),
        PostgresErrorCodes.NotNullViolation =>
            (StatusCodes.Status400BadRequest, TitleFor(400), "Falta un dato obligatorio."),
        _ => (StatusCodes.Status500InternalServerError, TitleFor(500), "Ocurrió un error inesperado. Intenta de nuevo más tarde."),
    };

    private static string TitleFor(int status) => status switch
    {
        400 => "Datos inválidos",
        404 => "No encontrado",
        409 => "Conflicto",
        422 => "Regla de negocio",
        _ => "Error del servidor",
    };
}
