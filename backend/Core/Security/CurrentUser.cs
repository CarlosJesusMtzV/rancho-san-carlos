using System.Security.Claims;

namespace TecNM.Api.Core.Security;

/// <summary>Usuario de la petición en curso; sin sesión (arranque, tareas internas) no tiene identidad ni permisos.</summary>
public interface ICurrentUser
{
    int? Id { get; }
    bool Has(string permission);
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string PermissionClaim = "perm";
    public const string MustChangePasswordClaim = "pwd_change";

    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int? Id => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool Has(string permission) => Principal?.HasClaim(PermissionClaim, permission) == true;
}
