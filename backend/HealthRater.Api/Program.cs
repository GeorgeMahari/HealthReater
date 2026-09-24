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
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors(DevCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { service = "HealthRater API", status = "running" }));

app.Run();

// Exposed for potential integration testing via WebApplicationFactory.
public partial class Program { }
