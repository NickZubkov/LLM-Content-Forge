using System.Net;
using System.Net.Http.Json;
using ContentForge.Api.Llm;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContentForge.Api.Tests;

public class GenerateEndpointTests
{
    private static WebApplicationFactory<Program> FactoryWith(ILlmClient llm) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILlmClient>();
                services.AddSingleton(llm);
            }));

    private static object ValidRequest() => new
    {
        contentType = "Item",
        count = 2,
        theme = "frozen dungeon",
        levelRange = new { min = 1, max = 10 },
    };

    [Fact]
    public async Task Returns_200_and_generated_content_on_success()
    {
        const string llmJson =
            """[{"name":"Frost Blade","description":"x","rarity":"rare","power":5,"value":100}]""";
        using var factory = FactoryWith(new StubLlmClient(llmJson));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/generate", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Frost Blade").And.Contain("power");
    }

    [Fact]
    public async Task Returns_400_on_invalid_input()
    {
        using var factory = FactoryWith(new StubLlmClient("[]"));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/generate", new
        {
            contentType = "Item",
            count = 0, // invalid
            theme = "frozen dungeon",
            levelRange = new { min = 1, max = 10 },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_502_when_llm_backend_fails()
    {
        using var factory = FactoryWith(new StubLlmClient(new LlmException("backend down")));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/generate", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Returns_502_when_llm_content_is_invalid()
    {
        using var factory = FactoryWith(new StubLlmClient("no json here"));
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/generate", ValidRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }
}

/// <summary>Test double for <see cref="ILlmClient"/>: returns a fixed body or throws.</summary>
internal sealed class StubLlmClient : ILlmClient
{
    private readonly string? _response;
    private readonly Exception? _error;

    public StubLlmClient(string response) => _response = response;
    public StubLlmClient(Exception error) => _error = error;

    public Task<string> CompleteAsync(LlmPrompt prompt, CancellationToken cancellationToken) =>
        _error is not null ? Task.FromException<string>(_error) : Task.FromResult(_response!);
}
