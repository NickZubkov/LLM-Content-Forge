var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger — exposed out of the box for exploring the API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Content Forge API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Liveness probe. Kept trivial on purpose — no dependencies to check yet.
app.MapGet("/api/v1/health", () => Results.Ok(new HealthResponse("ok")))
   .WithName("Health")
   .WithTags("System");

app.Run();

/// <summary>Response body for <c>GET /api/v1/health</c>.</summary>
public record HealthResponse(string Status);

// Exposed so the integration tests can spin up the app via WebApplicationFactory<Program>.
public partial class Program;
