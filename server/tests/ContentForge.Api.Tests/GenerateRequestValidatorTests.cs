using ContentForge.Api.Generation;
using ContentForge.Api.Validation;
using FluentAssertions;

namespace ContentForge.Api.Tests;

public class GenerateRequestValidatorTests
{
    private readonly GenerateRequestValidator _validator = new();

    private static GenerateRequest Valid() =>
        new(ContentType.Item, Count: 5, Theme: "frozen dungeon", LevelRange: new IntRange(1, 10));

    [Fact]
    public void Accepts_a_well_formed_request()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_non_positive_count()
    {
        var request = Valid() with { Count = 0 };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_empty_theme()
    {
        var request = Valid() with { Theme = "  " };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_inverted_range()
    {
        var request = Valid() with { LevelRange = new IntRange(10, 1) };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_undefined_content_type()
    {
        var request = Valid() with { ContentType = (ContentType)999 };

        _validator.Validate(request).IsValid.Should().BeFalse();
    }
}
