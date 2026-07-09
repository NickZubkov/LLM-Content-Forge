using System.Net.Http.Headers;
using ContentForge.Api.Llm;
using ContentForge.Api.Prompts;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI / Swagger — exposed out of the box for exploring the API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Content Forge API", Version = "v1" });
});

// LLM backend: provider-agnostic behind ILlmClient; defaults to a local Ollama via its
// OpenAI-compatible API. Base URL, model, timeout, and optional API key come from config/env.
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection(LlmOptions.SectionName));
builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
builder.Services.AddHttpClient<ILlmClient, OllamaLlmClient>((serviceProvider, http) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<LlmOptions>>().Value;
    var baseUrl = options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/";
    http.BaseAddress = new Uri(baseUrl);
    http.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    if (!string.IsNullOrWhiteSpace(options.ApiKey))
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }
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
public partial class Program {}
