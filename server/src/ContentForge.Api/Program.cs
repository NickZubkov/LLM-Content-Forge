using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using ContentForge.Api.Generation;
using ContentForge.Api.Llm;
using ContentForge.Api.Prompts;
using ContentForge.Api.Validation;
using FluentValidation;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

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
})
.AddResilienceHandler("llm", pipeline =>
{
    // Retry transient failures (5xx, 408, connection errors) with exponential backoff + jitter.
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 2,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromSeconds(1),
    });
});

// Request validation (FluentValidation) and the generation orchestrator.
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<IContentGenerator, ContentGenerator>();

// Accept and emit enums as strings ("Item"/"Enemy") in JSON.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Liveness probe. Kept trivial on purpose — no dependencies to check yet.
app.MapGet("/api/v1/health", () => Results.Ok(new HealthResponse("ok")))
   .WithName("Health")
   .WithTags("System");

app.MapPost("/api/v1/generate",
    async (GenerateRequest request, IContentGenerator generator, ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var result = await generator.GenerateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (LlmException ex)
        {
            logger.LogWarning(ex, "LLM backend call failed");
            return Results.Problem(
                title: "LLM backend error",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (ContentValidationException ex)
        {
            logger.LogWarning(ex, "LLM returned content that failed validation");
            return Results.Problem(
                title: "Invalid content from LLM",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    })
    .AddEndpointFilter<ValidationFilter<GenerateRequest>>()
    .WithName("Generate")
    .WithTags("Generation");

app.Run();

/// <summary>Response body for <c>GET /api/v1/health</c>.</summary>
public record HealthResponse(string Status);

// Exposed so the integration tests can spin up the app via WebApplicationFactory<Program>.
public partial class Program {}
