namespace TecNM.Api.Core.Entities;

/// <summary>Cuenta para entrar al panel. Sus roles están en <see cref="UserRoles"/>.</summary>
public sealed class User : AuditableEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool MustChangePassword { get; set; } = true;
    public short FailedLogins { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public List<UserRole> UserRoles { get; set; } = [];
}

/// <summary>Rol por negocio: general, ganadería o quesería.</summary>
public sealed class Role : AuditableEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Division { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>Rol asignado a una cuenta.</summary>
public sealed class UserRole : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
