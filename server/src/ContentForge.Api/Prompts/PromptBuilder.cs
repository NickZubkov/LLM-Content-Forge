using System.Globalization;
using System.Reflection;
using ContentForge.Api.Generation;
using ContentForge.Api.Llm;

namespace ContentForge.Api.Prompts;

/// <summary>
/// Builds prompts from embedded template files under <c>Prompts/Templates/</c>, substituting
/// <c>{{placeholder}}</c> tokens with request parameters. Templates are editable without
/// touching this logic.
/// </summary>
public sealed class PromptBuilder : IPromptBuilder
{
    private const string ResourcePrefix = "ContentForge.Api.Prompts.Templates.";
    private static readonly Assembly Assembly = typeof(PromptBuilder).Assembly;

    private readonly string _systemTemplate;
    private readonly IReadOnlyDictionary<ContentType, string> _userTemplates;

    public PromptBuilder()
    {
        _systemTemplate = ReadTemplate("system.md");
        _userTemplates = new Dictionary<ContentType, string>
        {
            [ContentType.Item] = ReadTemplate("item.md"),
            [ContentType.Enemy] = ReadTemplate("enemy.md"),
        };
    }

    public LlmPrompt Build(ContentType contentType, GenerationParameters parameters)
    {
        if (!_userTemplates.TryGetValue(contentType, out var userTemplate))
        {
            throw new ArgumentOutOfRangeException(
                nameof(contentType), contentType, "Unsupported content type.");
        }

        var replacements = new Dictionary<string, string>
        {
            ["content_type"] = contentType.ToString().ToLowerInvariant(),
            ["count"] = parameters.Count.ToString(CultureInfo.InvariantCulture),
            ["theme"] = parameters.Theme,
            ["level_min"] = parameters.LevelRange.Min.ToString(CultureInfo.InvariantCulture),
            ["level_max"] = parameters.LevelRange.Max.ToString(CultureInfo.InvariantCulture),
        };

        return new LlmPrompt(
            System: Fill(_systemTemplate, replacements),
            User: Fill(userTemplate, replacements));
    }

    private static string Fill(string template, IReadOnlyDictionary<string, string> replacements)
    {
        foreach (var (key, value) in replacements)
        {
            template = template.Replace("{{" + key + "}}", value);
        }

        return template;
    }

    private static string ReadTemplate(string fileName)
    {
        var resourceName = ResourcePrefix + fileName;
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded prompt template '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
