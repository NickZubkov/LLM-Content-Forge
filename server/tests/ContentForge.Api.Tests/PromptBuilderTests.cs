using ContentForge.Api.Generation;
using ContentForge.Api.Prompts;
using FluentAssertions;

namespace ContentForge.Api.Tests;

public class PromptBuilderTests
{
    private readonly PromptBuilder _builder = new();

    [Fact]
    public void Build_item_prompt_includes_parameters_and_leaves_no_placeholders()
    {
        var parameters = new GenerationParameters(
            Count: 5, Theme: "frozen dungeon", LevelRange: new IntRange(1, 10));

        var prompt = _builder.Build(ContentType.Item, parameters);

        prompt.System.Should().NotBeNullOrWhiteSpace();
        prompt.User.Should().Contain("5");
        prompt.User.Should().Contain("frozen dungeon");
        prompt.User.Should().Contain("10");
        prompt.User.Should().Contain("item");
        prompt.User.Should().NotContain("{{");
        prompt.System.Should().NotContain("{{");
    }

    [Fact]
    public void Build_enemy_prompt_targets_enemy_fields()
    {
        var parameters = new GenerationParameters(3, "haunted forest", new IntRange(2, 8));

        var prompt = _builder.Build(ContentType.Enemy, parameters);

        prompt.User.Should().Contain("haunted forest");
        prompt.User.Should().Contain("health");
        prompt.User.Should().Contain("damage");
        prompt.User.Should().NotContain("{{");
    }
}
