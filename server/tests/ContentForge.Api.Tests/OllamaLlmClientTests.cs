using System.Net;
using System.Text;
using ContentForge.Api.Llm;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace ContentForge.Api.Tests;

public class OllamaLlmClientTests
{
    private static OllamaLlmClient CreateClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:11434/v1/") };
        var options = Options.Create(new LlmOptions { Model = "test-model" });
        return new OllamaLlmClient(http, options);
    }

    [Fact]
    public async Task Returns_message_content_on_success()
    {
        const string body =
            """{"choices":[{"message":{"role":"assistant","content":"[{\"name\":\"Frost Blade\"}]"}}]}""";
        var client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.CompleteAsync(new LlmPrompt("sys", "user"), CancellationToken.None);

        result.Should().Contain("Frost Blade");
    }

    [Fact]
    public async Task Throws_LlmException_on_error_status()
    {
        var client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "boom"));

        var act = () => client.CompleteAsync(new LlmPrompt("s", "u"), CancellationToken.None);

        await act.Should().ThrowAsync<LlmException>().WithMessage("*500*");
    }

    [Fact]
    public async Task Throws_LlmException_on_empty_choices()
    {
        var client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, """{"choices":[]}"""));

        var act = () => client.CompleteAsync(new LlmPrompt("s", "u"), CancellationToken.None);

        await act.Should().ThrowAsync<LlmException>();
    }

    [Fact]
    public async Task Propagates_caller_cancellation()
    {
        var client = CreateClient(new StubHttpMessageHandler(HttpStatusCode.OK, """{"choices":[]}"""));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => client.CompleteAsync(new LlmPrompt("s", "u"), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

/// <summary>Returns a fixed status + body for any request; honours cancellation.</summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
