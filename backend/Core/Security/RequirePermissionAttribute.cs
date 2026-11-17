using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TecNM.Api.Core.Security;

/// <summary>Exige sesión y al menos uno de los permisos indicados; si no, responde 403 en español.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute(params string[] anyOf) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return; // La política general ya responde 401.
        }

        if (!anyOf.Any(permission => user.HasClaim(CurrentUser.PermissionClaim, permission)))
        {
            context.Result = AccessProblems.Forbidden(context.HttpContext, "Tu rol no permite esta acción.");
        }
    }
}

public static class AccessProblems
{
    public static ObjectResult Forbidden(HttpContext http, string detail) => Problem(http, StatusCodes.Status403Forbidden, "Sin permiso", detail);

    public static ObjectResult Problem(HttpContext http, int status, string title, string detail) =>
        new(new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = http.Request.Path })
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" },
        };

    public static Task WriteAsync(HttpContext http, int status, string title, string detail)
    {
        http.Response.StatusCode = status;
        return http.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = detail, Instance = http.Request.Path },
            options: null,
            contentType: "application/problem+json");
    }
}
