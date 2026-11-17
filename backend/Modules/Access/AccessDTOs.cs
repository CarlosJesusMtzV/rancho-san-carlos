namespace TecNM.Api.Modules.Access;

public sealed record RoleItem(string Code, string Name, string Division, string Description);

public sealed record SessionInfo(int Id, string Username, string FullName, IReadOnlyList<RoleItem> Roles, IReadOnlyList<string> Permissions, bool MustChangePassword);

public sealed record UserItem(
    int Id,
    string Username,
    string FullName,
    string? Email,
    IReadOnlyList<RoleItem> Roles,
    bool Active,
    bool MustChangePassword,
    DateTime? LastLoginAt,
    DateTime? LockedUntil);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

public sealed record SaveUserRequest(string? Username, string? FullName, string? Email, IReadOnlyList<string>? RoleCodes, bool? Active, string? Password);

public sealed record ResetPasswordRequest(string? Password);
