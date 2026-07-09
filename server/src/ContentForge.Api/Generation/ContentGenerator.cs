using System.Text.Json;
using ContentForge.Api.Llm;
using ContentForge.Api.Prompts;

namespace ContentForge.Api.Generation;

/// <summary>
/// Default <see cref="IContentGenerator"/>: builds a prompt, calls the LLM, extracts the JSON
/// array from the (possibly noisy) response, deserializes it into typed content, and validates
/// every entry against the requested value ranges. Any parse or range failure surfaces as a
/// <see cref="ContentValidationException"/>.
/// </summary>
public sealed class ContentGenerator : IContentGenerator
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> AllowedRarities =
        new(StringComparer.OrdinalIgnoreCase) { "common", "uncommon", "rare", "epic", "legendary" };

    private readonly ILlmClient _llm;
    private readonly IPromptBuilder _promptBuilder;

    public ContentGenerator(ILlmClient llm, IPromptBuilder promptBuilder)
    {
        _llm = llm;
        _promptBuilder = promptBuilder;
    }

    public async Task<GenerateResponse> GenerateAsync(GenerateRequest request, CancellationToken cancellationToken)
    {
        var parameters = new GenerationParameters(request.Count, request.Theme, request.LevelRange);
        var prompt = _promptBuilder.Build(request.ContentType, parameters);

        var raw = await _llm.CompleteAsync(prompt, cancellationToken);
        var json = ExtractJsonArray(raw);

        IReadOnlyList<object> content = request.ContentType switch
        {
            ContentType.Item => Validate(Deserialize<GeneratedItem>(json), request.LevelRange),
            ContentType.Enemy => Validate(Deserialize<GeneratedEnemy>(json), request.LevelRange),
            _ => throw new ContentValidationException($"Unsupported content type '{request.ContentType}'."),
        };

        return new GenerateResponse(request.ContentType, content);
    }

    /// <summary>
    /// Slices the outermost JSON array out of the raw completion, tolerating prose or markdown
    /// code fences that small local models sometimes add despite instructions.
    /// </summary>
    private static string ExtractJsonArray(string raw)
    {
        var start = raw.IndexOf('[');
        var end = raw.LastIndexOf(']');
        if (start < 0 || end < start)
        {
            throw new ContentValidationException("LLM response did not contain a JSON array.");
        }

        return raw[start..(end + 1)];
    }

    private static List<T> Deserialize<T>(string json)
    {
        List<T>? list;
        try
        {
            list = JsonSerializer.Deserialize<List<T>>(json, Json);
        }
        catch (JsonException ex)
        {
            throw new ContentValidationException("LLM returned content that was not valid JSON.", ex);
        }

        if (list is null || list.Count == 0)
        {
            throw new ContentValidationException("LLM returned no content.");
        }

        return list;
    }

    private static IReadOnlyList<object> Validate(List<GeneratedItem> items, IntRange range)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                throw new ContentValidationException("An item is missing a name.");
            }

            if (string.IsNullOrWhiteSpace(item.Rarity) || !AllowedRarities.Contains(item.Rarity))
            {
                throw new ContentValidationException($"Item '{item.Name}' has invalid rarity '{item.Rarity}'.");
            }

            if (item.Power < range.Min || item.Power > range.Max)
            {
                throw new ContentValidationException(
                    $"Item '{item.Name}' power {item.Power} is outside [{range.Min}, {range.Max}].");
            }

            if (item.Value < 0)
            {
                throw new ContentValidationException($"Item '{item.Name}' has a negative value.");
            }
        }

        return items.Cast<object>().ToArray();
    }

    private static IReadOnlyList<object> Validate(List<GeneratedEnemy> enemies, IntRange range)
    {
        foreach (var enemy in enemies)
        {
            if (string.IsNullOrWhiteSpace(enemy.Name))
            {
                throw new ContentValidationException("An enemy is missing a name.");
            }

            if (enemy.Level < range.Min || enemy.Level > range.Max)
            {
                throw new ContentValidationException(
                    $"Enemy '{enemy.Name}' level {enemy.Level} is outside [{range.Min}, {range.Max}].");
            }

            if (enemy.Health <= 0)
            {
                throw new ContentValidationException($"Enemy '{enemy.Name}' must have positive health.");
            }

            if (enemy.Damage < 0)
            {
                throw new ContentValidationException($"Enemy '{enemy.Name}' has negative damage.");
            }
        }

        return enemies.Cast<object>().ToArray();
    }
}
