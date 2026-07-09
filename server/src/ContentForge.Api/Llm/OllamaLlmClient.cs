using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ContentForge.Api.Llm;

/// <summary>
/// <see cref="ILlmClient"/> backed by an OpenAI-compatible chat-completions endpoint
/// (Ollama by default). Talks raw HTTP, so switching to a cloud OpenAI-compatible provider
/// is a base-URL change.
/// </summary>
public sealed class OllamaLlmClient : ILlmClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly LlmOptions _options;

    public OllamaLlmClient(HttpClient http, IOptions<LlmOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string> CompleteAsync(LlmPrompt prompt, CancellationToken cancellationToken)
    {
        var request = new ChatRequest(
            Model: _options.Model,
            Messages:
            [
                new ChatMessage("system", prompt.System),
                new ChatMessage("user", prompt.User)
            ],
            Temperature: _options.Temperature,
            Stream: false);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsJsonAsync("chat/completions", request, Json, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // genuine caller cancellation — let it propagate unchanged
        }
        catch (OperationCanceledException ex)
        {
            throw new LlmException($"LLM request timed out after {_options.TimeoutSeconds}s.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new LlmException($"Could not reach the LLM backend at '{_http.BaseAddress}'.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new LlmException(
                $"LLM backend returned {(int)response.StatusCode}: {Truncate(body, 500)}");
        }

        ChatResponse? parsed;
        try
        {
            parsed = await response.Content.ReadFromJsonAsync<ChatResponse>(Json, cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new LlmException("LLM backend returned a response that was not valid JSON.", ex);
        }

        var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new LlmException("LLM backend returned an empty completion.");
        }

        return content;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";

    // OpenAI-compatible wire DTOs (only the subset we use). Web JSON options handle the
    // camelCase field names automatically.
    private sealed record ChatRequest(
        string Model,
        IReadOnlyList<ChatMessage> Messages,
        double Temperature,
        bool Stream);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ChatResponse(IReadOnlyList<Choice>? Choices);

    private sealed record Choice(ChatMessage? Message);
}
