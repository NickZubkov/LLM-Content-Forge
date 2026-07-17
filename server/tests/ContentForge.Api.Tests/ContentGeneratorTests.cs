using ContentForge.Api.Generation;
using ContentForge.Api.Prompts;
using FluentAssertions;

namespace ContentForge.Api.Tests;

public class ContentGeneratorTests
{
    private static ContentGenerator CreateGenerator(string llmResponse) =>
        new(new StubLlmClient(llmResponse), new PromptBuilder());

    private static GenerateRequest ItemRequest(IntRange range) =>
        new(ContentType.Item, Count: 1, Theme: "frozen dungeon", LevelRange: range);

    [Fact]
    public async Task Parses_and_returns_valid_items()
    {
        const string response =
            """[{"name":"Frost Blade","description":"Chills foes.","rarity":"rare","power":5,"value":100}]""";
        var generator = CreateGenerator(response);

        var result = await generator.GenerateAsync(ItemRequest(new IntRange(1, 10)), CancellationToken.None);

        result.ContentType.Should().Be(ContentType.Item);
        result.Content.Should().ContainSingle();
        var item = result.Content[0].Should().BeOfType<GeneratedItem>().Subject;
        item.Name.Should().Be("Frost Blade");
        item.Power.Should().Be(5);
    }

    [Fact]
    public async Task Tolerates_prose_and_markdown_fences_around_the_array()
    {
        const string response =
            "Sure, here you go:\n```json\n[{\"name\":\"Ember\",\"description\":\"x\",\"rarity\":\"common\",\"power\":2,\"value\":10}]\n```";
        var generator = CreateGenerator(response);

        var result = await generator.GenerateAsync(ItemRequest(new IntRange(1, 10)), CancellationToken.None);

        result.Content.Should().ContainSingle();
    }

    [Fact]
    public async Task Throws_when_response_has_no_json_array()
    {
        var generator = CreateGenerator("I cannot do that.");

        var act = () => generator.GenerateAsync(ItemRequest(new IntRange(1, 10)), CancellationToken.None);

        await act.Should().ThrowAsync<ContentValidationException>();
    }

    [Fact]
    public async Task Throws_when_json_is_malformed()
    {
        var generator = CreateGenerator("[{ broken json ]");

        var act = () => generator.GenerateAsync(ItemRequest(new IntRange(1, 10)), CancellationToken.None);

        await act.Should().ThrowAsync<ContentValidationException>();
    }

    [Fact]
    public async Task Throws_when_item_power_is_out_of_range()
    {
        const string response =
            """[{"name":"Overlord Axe","description":"x","rarity":"epic","power":999,"value":100}]""";
        var generator = CreateGenerator(response);

        var act = () => generator.GenerateAsync(ItemRequest(new IntRange(1, 10)), CancellationToken.None);

        await act.Should().ThrowAsync<ContentValidationException>().WithMessage("*power*");
    }

    [Fact]
    public async Task Throws_when_item_rarity_is_invalid()
    {
        const string response =
            """[{"name":"Weird Ring","description":"x","rarity":"mythic","power":5,"value":100}]""";
        var generator = CreateGenerator(response);

        var act = () => generator.GenerateAsync(ItemRequest(new IntRange(1, 10)), CancellationToken.None);

        await act.Should().ThrowAsync<ContentValidationException>().WithMessage("*rarity*");
    }

    [Fact]
    public async Task Parses_and_returns_valid_enemies()
    {
        const string response =
            """[{"name":"Frost Ghoul","description":"Lurks in ice.","level":3,"health":30,"damage":5}]""";
        var generator = CreateGenerator(response);
        var request = new GenerateRequest(ContentType.Enemy, 1, "frozen dungeon", new IntRange(2, 8));

        var result = await generator.GenerateAsync(request, CancellationToken.None);

        result.ContentType.Should().Be(ContentType.Enemy);
        var enemy = result.Content[0].Should().BeOfType<GeneratedEnemy>().Subject;
        enemy.Level.Should().Be(3);
    }
}
