using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TecNM.Api.Modules.Access;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AccessService access) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<SessionInfo> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await access.LoginAsync(request, cancellationToken);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AccessService.ToCookiePrincipal(user));
        return AccessService.ToSession(user);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    public Task<SessionInfo> Me(CancellationToken cancellationToken) => access.GetSessionAsync(cancellationToken);

    [HttpPost("change-password")]
    public async Task<SessionInfo> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await access.ChangePasswordAsync(request, cancellationToken);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, AccessService.ToCookiePrincipal(user));
        return AccessService.ToSession(user);
    }
}
