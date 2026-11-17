using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Errors;
using TecNM.Api.Core.Security;
using TecNM.Api.Data;

namespace TecNM.Api.Modules.Access;

public sealed partial class AccessService(TecNMDbContext db, ICurrentUser currentUser)
{
    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockTime = TimeSpan.FromMinutes(15);
    private static readonly PasswordHasher<User> Hasher = new();

    // Hash de relleno: iguala el tiempo de respuesta cuando el usuario no existe.
    private static readonly string DummyHash = Hasher.HashPassword(new User(), "relleno-sin-uso-0");

    // ------------------------------------------------------------------ Sesión

    /// <summary>Valida usuario y contraseña. No revela si el usuario existe.</summary>
    public async Task<User> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim().ToLowerInvariant();
        var password = request.Password;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw ApiException.BadRequest("Escribe tu usuario y tu contraseña.");
        }

        var user = await UsersWithRoles().FirstOrDefaultAsync(item => item.Username == username && item.Status == 1, cancellationToken);
        if (user is null)
        {
            Hasher.VerifyHashedPassword(new User(), DummyHash, password);
            throw ApiException.Unauthorized("Usuario o contraseña incorrectos.");
        }

        var now = DateTime.UtcNow;
        if (user.LockedUntil is { } lockedUntil && lockedUntil > now)
        {
            var minutes = Math.Max(1, (int)Math.Ceiling((lockedUntil - now).TotalMinutes));
            throw ApiException.Locked($"La cuenta está bloqueada por intentos fallidos. Intenta de nuevo en {minutes} min o pide al dueño que restablezca tu contraseña.");
        }

        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLogins++;
            if (user.FailedLogins >= MaxFailedLogins)
            {
                user.FailedLogins = 0;
                user.LockedUntil = now.Add(LockTime);
            }

            await db.SaveChangesAsync(cancellationToken);
            throw ApiException.Unauthorized("Usuario o contraseña incorrectos.");
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = Hasher.HashPassword(user, password);
        }

        user.FailedLogins = 0;
        user.LockedUntil = null;
        user.LastLoginAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<SessionInfo> GetSessionAsync(CancellationToken cancellationToken)
    {
        var user = await LoadActiveAsync(currentUser.Id, cancellationToken)
            ?? throw ApiException.Unauthorized("Inicia sesión para continuar.");
        return ToSession(user);
    }

    public async Task<User> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var id = currentUser.Id ?? throw ApiException.Unauthorized("Inicia sesión para continuar.");
        var user = await UsersWithRoles().FirstOrDefaultAsync(item => item.Id == id && item.Status == 1, cancellationToken)
            ?? throw ApiException.Unauthorized("Inicia sesión para continuar.");
        if (string.IsNullOrEmpty(request.CurrentPassword)
            || Hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            throw ApiException.BadRequest("La contraseña actual no es correcta.");
        }

        var password = ValidatePassword(request.NewPassword);
        if (password == request.CurrentPassword)
        {
            throw ApiException.BadRequest("La contraseña nueva debe ser distinta de la actual.");
        }

        user.PasswordHash = Hasher.HashPassword(user, password);
        user.MustChangePassword = false;
        await db.SaveChangesAsync(cancellationToken);
        return user;
    }

    /// <summary>Cuenta activa con sus roles, o null si ya no existe o está desactivada.</summary>
    public Task<User?> LoadActiveAsync(int? id, CancellationToken cancellationToken) =>
        id is null
            ? Task.FromResult<User?>(null)
            : UsersWithRoles().AsNoTracking().FirstOrDefaultAsync(item => item.Id == id && item.Status == 1, cancellationToken);

    /// <summary>Identidad que se guarda en la cookie: solo el id. Los permisos se cargan en cada petición.</summary>
    public static ClaimsPrincipal ToCookiePrincipal(User user) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))],
            CookieAuthenticationDefaults.AuthenticationScheme));

    /// <summary>Identidad de la petición: id, nombre, permisos vigentes y aviso de contraseña temporal.</summary>
    public static ClaimsPrincipal ToPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.Username),
        };
        if (user.MustChangePassword)
        {
            claims.Add(new Claim(CurrentUser.MustChangePasswordClaim, "1"));
        }

        claims.AddRange(Permissions.For(user.UserRoles.Select(item => item.Role.Code)).Select(permission => new Claim(CurrentUser.PermissionClaim, permission)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static SessionInfo ToSession(User user) => new(
        user.Id,
        user.Username,
        user.FullName,
        ToRoles(user),
        Permissions.For(user.UserRoles.Select(item => item.Role.Code)),
        user.MustChangePassword);

    // ------------------------------------------------------------------ Usuarios

    public async Task<IReadOnlyList<RoleItem>> ListRolesAsync(CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking().OrderBy(role => role.Id)
            .Select(role => new RoleItem(role.Code, role.Name, role.Division, role.Description))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserItem>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await UsersWithRoles().AsNoTracking().OrderBy(user => user.FullName).ToListAsync(cancellationToken);
        return users.Select(ToItem).ToList();
    }

    public async Task<UserItem> CreateUserAsync(SaveUserRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!UsernamePattern().IsMatch(username))
        {
            throw ApiException.BadRequest("El usuario lleva de 3 a 32 caracteres: minúsculas, números, punto, guion o guion bajo.");
        }

        if (await db.Users.IgnoreQueryFilters().AnyAsync(item => item.Username == username, cancellationToken))
        {
            throw ApiException.Conflict($"Ya existe el usuario {username}.");
        }

        var user = new User { Username = username, MustChangePassword = true };
        user.PasswordHash = Hasher.HashPassword(user, ValidatePassword(request.Password));
        await ApplyProfileAsync(user, request, cancellationToken);
        var roles = await ResolveRolesAsync(request.RoleCodes, cancellationToken);
        user.UserRoles = roles.Select(role => new UserRole { RoleId = role.Id }).ToList();
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(user.Id, cancellationToken);
    }

    public async Task<UserItem> UpdateUserAsync(int id, SaveUserRequest request, CancellationToken cancellationToken)
    {
        var user = await UsersWithRoles().FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El usuario no existe.");
        await ApplyProfileAsync(user, request, cancellationToken);
        var roles = await ResolveRolesAsync(request.RoleCodes, cancellationToken);
        var active = request.Active ?? user.Status == 1;
        var keepsOwner = active && roles.Any(role => role.Code == Permissions.RoleOwner);
        var wasOwner = user.Status == 1 && user.UserRoles.Any(item => item.Role.Code == Permissions.RoleOwner);

        if (id == currentUser.Id && (!active || (wasOwner && !keepsOwner)))
        {
            throw ApiException.Conflict("No puedes desactivar tu propia cuenta ni quitarte el rol de dueño; pídeselo a otro dueño.");
        }

        if (wasOwner && !keepsOwner && !await db.UserRoles.AnyAsync(
                item => item.UserId != id && item.Role.Code == Permissions.RoleOwner && item.User.Status == 1, cancellationToken))
        {
            throw ApiException.Conflict("Debe quedar al menos un dueño activo.");
        }

        var now = DateTime.UtcNow;
        var wanted = roles.Select(role => role.Id).ToHashSet();
        foreach (var removed in user.UserRoles.Where(item => !wanted.Contains(item.RoleId)).ToList())
        {
            removed.DeletedAt = now;
            removed.Status = 0;
        }

        var current = user.UserRoles.Select(item => item.RoleId).ToHashSet();
        user.UserRoles.AddRange(roles.Where(role => !current.Contains(role.Id)).Select(role => new UserRole { RoleId = role.Id }));
        user.Status = (short)(active ? 1 : 0);
        if (active)
        {
            user.FailedLogins = 0;
            user.LockedUntil = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(id, cancellationToken);
    }

    public async Task<UserItem> ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw ApiException.NotFound("El usuario no existe.");
        user.PasswordHash = Hasher.HashPassword(user, ValidatePassword(request.Password));
        user.MustChangePassword = true;
        user.FailedLogins = 0;
        user.LockedUntil = null;
        await db.SaveChangesAsync(cancellationToken);
        return await GetUserAsync(id, cancellationToken);
    }

    /// <summary>Crea el primer dueño si no hay ninguna cuenta. Devuelve true si lo creó.</summary>
    public async Task<bool> BootstrapOwnerAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return false;
        }

        await CreateUserAsync(new SaveUserRequest(username, "Dueño", null, [Permissions.RoleOwner], true, password), cancellationToken);
        return true;
    }

    // ------------------------------------------------------------------ Utilidades

    private IQueryable<User> UsersWithRoles() => db.Users.Include(user => user.UserRoles).ThenInclude(item => item.Role);

    private async Task<UserItem> GetUserAsync(int id, CancellationToken cancellationToken) =>
        ToItem(await UsersWithRoles().AsNoTracking().FirstAsync(user => user.Id == id, cancellationToken));

    private async Task ApplyProfileAsync(User user, SaveUserRequest request, CancellationToken cancellationToken)
    {
        var fullName = TextInput.Clean(request.FullName) ?? throw ApiException.BadRequest("Escribe el nombre completo.");
        if (fullName.Length > 120)
        {
            throw ApiException.BadRequest("El nombre admite hasta 120 caracteres.");
        }

        var email = TextInput.Clean(request.Email)?.ToLowerInvariant();
        if (email is not null && (email.Length > 254 || !email.Contains('@')))
        {
            throw ApiException.BadRequest("El correo no es válido.");
        }

        if (email is not null && await db.Users.IgnoreQueryFilters().AnyAsync(item => item.Email == email && item.Id != user.Id, cancellationToken))
        {
            throw ApiException.Conflict($"El correo {email} ya está en otra cuenta.");
        }

        user.FullName = fullName;
        user.Email = email;
    }

    private async Task<List<Role>> ResolveRolesAsync(IReadOnlyList<string>? codes, CancellationToken cancellationToken)
    {
        var wanted = (codes ?? []).Select(code => code.Trim().ToLowerInvariant()).Distinct().ToList();
        if (wanted.Count == 0)
        {
            throw ApiException.BadRequest("Elige al menos un rol.");
        }

        var roles = await db.Roles.Where(role => wanted.Contains(role.Code)).ToListAsync(cancellationToken);
        return roles.Count == wanted.Count ? roles : throw ApiException.BadRequest("Uno de los roles no existe.");
    }

    private static string ValidatePassword(string? password)
    {
        if (password is null || password.Length is < 8 or > 128 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw ApiException.BadRequest("La contraseña lleva de 8 a 128 caracteres, con al menos una letra y un número.");
        }

        return password;
    }

    private static List<RoleItem> ToRoles(User user) =>
        user.UserRoles.OrderBy(item => item.RoleId).Select(item => new RoleItem(item.Role.Code, item.Role.Name, item.Role.Division, item.Role.Description)).ToList();

    private static UserItem ToItem(User user) =>
        new(user.Id, user.Username, user.FullName, user.Email, ToRoles(user), user.Status == 1, user.MustChangePassword, user.LastLoginAt,
            user.LockedUntil is { } locked && locked > DateTime.UtcNow ? locked : null);

    [GeneratedRegex("^[a-z0-9._-]{3,32}$")]
    private static partial Regex UsernamePattern();
}
