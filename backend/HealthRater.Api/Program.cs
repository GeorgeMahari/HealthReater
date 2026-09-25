using System.Security.Claims;
using System.Threading.RateLimiting;
using HealthRater.Api.Controllers;
using HealthRater.Data;
using HealthRater.Data.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string DevCorsPolicy = "DevCors";

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // the auth cookie must travel with fetch(..., { credentials: "include" })
    });
});

// ---------- Database ----------
// Database:Provider selects the EF Core provider ("Sqlite" by default, or "SqlServer").
// Each provider has its own DbContext subclass + migrations; the app depends only on
// HealthRaterDbContext. Connection strings come from configuration (appsettings,
// environment variables such as ConnectionStrings__SqlServer, or user-secrets).
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    var connectionString = builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException(
            "Database:Provider is SqlServer but ConnectionStrings:SqlServer is not set (use an environment variable or user-secrets).");
    builder.Services.AddDbContext<SqlServerHealthRaterDbContext>(o => o.UseSqlServer(connectionString));
    builder.Services.AddScoped<HealthRaterDbContext>(sp => sp.GetRequiredService<SqlServerHealthRaterDbContext>());
}
else if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    // Relative SQLite paths are resolved against the API folder, not the shell's working directory.
    var sqlite = new SqliteConnectionStringBuilder(
        builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=App_Data/healthrater.db");
    if (!Path.IsPathRooted(sqlite.DataSource) && sqlite.DataSource != ":memory:")
    {
        sqlite.DataSource = Path.Combine(builder.Environment.ContentRootPath, sqlite.DataSource);
        Directory.CreateDirectory(Path.GetDirectoryName(sqlite.DataSource)!);
    }
    builder.Services.AddDbContext<SqliteHealthRaterDbContext>(o => o.UseSqlite(sqlite.ToString()));
    builder.Services.AddScoped<HealthRaterDbContext>(sp => sp.GetRequiredService<SqliteHealthRaterDbContext>());
}
else
{
    throw new InvalidOperationException($"Unsupported Database:Provider '{provider}'. Use 'Sqlite' or 'SqlServer'.");
}

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AssessmentService>();
builder.Services.AddScoped<ProfileService>();

// ---------- Authentication ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "healthrater.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        // This is an API: answer 401/403 instead of redirecting to a login page.
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        // Every request re-checks the server-side session, so logout (or a deactivated
        // account) takes effect immediately even if the old cookie is replayed.
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var sessions = ctx.HttpContext.RequestServices.GetRequiredService<SessionService>();
            var userId = await sessions.ValidateAsync(ctx.Principal?.FindFirstValue(AuthController.SessionClaim));
            if (userId is null || userId.ToString() != ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                ctx.RejectPrincipal();
                await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

// Slow down password guessing: 10 register/login attempts per minute per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthController.RateLimitPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// Development convenience: bring the database schema up to date on startup (and seed
// demo data only when explicitly enabled). Production applies migrations explicitly
// with `dotnet ef database update` or a migration bundle — see README.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HealthRaterDbContext>();
    await db.Database.MigrateAsync();

    if (app.Configuration.GetValue("Database:SeedDevelopmentData", false))
    {
        await DevelopmentSeeder.SeedAsync(db);
    }
}

app.UseCors(DevCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { service = "HealthRater API", status = "running" }));

app.Run();

// Exposed for potential integration testing via WebApplicationFactory.
public partial class Program { }
