using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Errors;
using TecNM.Api.Core.Security;
using TecNM.Api.Data;
using TecNM.Api.Modules.Access;
using TecNM.Api.Modules.AnimalHealth;
using TecNM.Api.Modules.Cheese;
using TecNM.Api.Modules.Herd;
using TecNM.Api.Modules.Milking;
using TecNM.Api.Modules.Reproduction;
using TecNM.Api.Modules.Sales;
using TecNM.Api.Modules.Slaughter;

const string FrontendCors = "Frontend";

LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta ConnectionStrings__DefaultConnection en la configuración.");
}

var allowedOrigins = (configuration["Cors:AllowedOrigins"] ?? configuration["Cors:AllowedOrigin"])
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .ToArray();

if (allowedOrigins is null || allowedOrigins.Length == 0)
{
    if (builder.Environment.IsDevelopment())
    {
        allowedOrigins = ["https://localhost:5173", "http://localhost:5173"];
    }
    else
    {
        throw new InvalidOperationException(
            "Cors__AllowedOrigin o Cors__AllowedOrigins es obligatorio en producción.");
    }
}

foreach (var origin in allowedOrigins)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
        || (!builder.Environment.IsDevelopment() && originUri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException(
            builder.Environment.IsDevelopment()
                ? $"El origen CORS '{origin}' debe ser una URI absoluta válida."
                : $"El origen CORS '{origin}' debe ser un origen HTTPS absoluto.");
    }
}

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
    // Datos con formato inválido (JSON mal formado, fechas o números ilegibles): mensaje único en español.
    options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(new ProblemDetails
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Datos inválidos",
        Detail = "Revisa los datos: hay campos vacíos o con formato inválido.",
        Instance = context.HttpContext.Request.Path,
    })
    {
        ContentTypes = { "application/problem+json" },
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<AccessService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "rsc_session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = true;
    // API: sin redirecciones; 401 y 403 en español.
    options.Events.OnRedirectToLogin = context =>
        AccessProblems.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized, "Sin sesión", "Inicia sesión para continuar.");
    options.Events.OnRedirectToAccessDenied = context =>
        AccessProblems.WriteAsync(context.HttpContext, StatusCodes.Status403Forbidden, "Sin permiso", "Tu rol no permite esta acción.");
    // Cada petición toma los roles vigentes: desactivar una cuenta o cambiar sus roles surte efecto de inmediato.
    options.Events.OnValidatePrincipal = async context =>
    {
        var access = context.HttpContext.RequestServices.GetRequiredService<AccessService>();
        var id = int.TryParse(context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var value) ? value : (int?)null;
        var user = await access.LoadActiveAsync(id, context.HttpContext.RequestAborted);
        if (user is null)
        {
            context.RejectPrincipal();
            return;
        }

        context.ReplacePrincipal(AccessService.ToPrincipal(user));
    };
});
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddScoped<HerdService>();
builder.Services.AddScoped<AnimalHealthService>();
builder.Services.AddScoped<ReproductionService>();
builder.Services.AddScoped<MilkingService>();
builder.Services.AddScoped<CheeseService>();
builder.Services.AddScoped<SlaughterService>();
builder.Services.AddScoped<SalesService>();
builder.Services.AddDbContext<TecNMDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddCors(options => options.AddPolicy(FrontendCors, policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseCors(FrontendCors);

// Defensa contra peticiones de otros sitios: lo que modifica datos solo se acepta del propio origen.
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers.Origin.ToString();
    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method)
        && origin.Length > 0
        && !allowedOrigins.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase)
        && !(Uri.TryCreate(origin, UriKind.Absolute, out var originUri) && string.Equals(originUri.Authority, context.Request.Host.Value, StringComparison.OrdinalIgnoreCase)))
    {
        await AccessProblems.WriteAsync(context, StatusCodes.Status403Forbidden, "Sin permiso", "La petición viene de un sitio no autorizado.");
        return;
    }

    await next();
});

app.UseAuthentication();

// Con contraseña temporal solo se puede usar /api/auth hasta cambiarla.
app.Use(async (context, next) =>
{
    if (context.User.HasClaim(CurrentUser.MustChangePasswordClaim, "1") && !context.Request.Path.StartsWithSegments("/api/auth"))
    {
        await AccessProblems.WriteAsync(context, StatusCodes.Status403Forbidden, "Sin permiso", "Cambia tu contraseña temporal para continuar.");
        return;
    }

    await next();
});

app.UseAuthorization();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();

await BootstrapOwnerAsync(app);

app.Run();

// Primer dueño: solo si no hay ninguna cuenta y se configuraron sus credenciales iniciales.
static async Task BootstrapOwnerAsync(WebApplication app)
{
    var username = app.Configuration["Bootstrap:OwnerUsername"];
    var password = app.Configuration["Bootstrap:OwnerPassword"];
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    try
    {
        using var scope = app.Services.CreateScope();
        if (await scope.ServiceProvider.GetRequiredService<AccessService>().BootstrapOwnerAsync(username, password, CancellationToken.None))
        {
            app.Logger.LogInformation("Se creó el dueño inicial {Username}; su contraseña es temporal.", username);
        }
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "No se pudo crear el dueño inicial. Si la base es anterior, aplica docs/migrations/2026-11-16-07-acceso.sql y reinicia.");
    }
}

static void LoadDotEnv()
{
    var searchDir = new DirectoryInfo(Directory.GetCurrentDirectory());
    DirectoryInfo? backendDir = null;

    if (File.Exists(Path.Combine(searchDir.FullName, "TecNM.Api.csproj")))
    {
        backendDir = searchDir;
    }
    else if (Directory.Exists(Path.Combine(searchDir.FullName, "backend")))
    {
        backendDir = new DirectoryInfo(Path.Combine(searchDir.FullName, "backend"));
    }
    else
    {
        var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (baseDir != null)
        {
            if (File.Exists(Path.Combine(baseDir.FullName, "TecNM.Api.csproj")))
            {
                backendDir = baseDir;
                break;
            }

            baseDir = baseDir.Parent;
        }
    }

    if (backendDir == null)
    {
        return;
    }

    var envPath = Path.Combine(backendDir.FullName, ".env");
    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var rawLine in File.ReadAllLines(envPath))
    {
        var line = rawLine.Trim();
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim().Trim('"', '\'');

        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}
