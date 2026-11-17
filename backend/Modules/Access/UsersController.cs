using Microsoft.AspNetCore.Mvc;
using TecNM.Api.Core.Security;

namespace TecNM.Api.Modules.Access;

[ApiController]
[Route("api/users")]
[RequirePermission(Permissions.UsersManage)]
public sealed class UsersController(AccessService access) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UserItem>> List(CancellationToken cancellationToken) => access.ListUsersAsync(cancellationToken);

    [HttpGet("roles")]
    public Task<IReadOnlyList<RoleItem>> ListRoles(CancellationToken cancellationToken) => access.ListRolesAsync(cancellationToken);

    [HttpPost]
    public async Task<ActionResult<UserItem>> Create(SaveUserRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await access.CreateUserAsync(request, cancellationToken));

    [HttpPut("{id:int}")]
    public Task<UserItem> Update(int id, SaveUserRequest request, CancellationToken cancellationToken) =>
        access.UpdateUserAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/reset-password")]
    public Task<UserItem> ResetPassword(int id, ResetPasswordRequest request, CancellationToken cancellationToken) =>
        access.ResetPasswordAsync(id, request, cancellationToken);
}
