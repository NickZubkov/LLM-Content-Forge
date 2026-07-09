using ContentForge.Api.Generation;
using FluentValidation;

namespace ContentForge.Api.Validation;

/// <summary>Validates the caller-supplied <see cref="GenerateRequest"/> before any LLM call.</summary>
public sealed class GenerateRequestValidator : AbstractValidator<GenerateRequest>
{
    public GenerateRequestValidator()
    {
        RuleFor(request => request.Count)
            .InclusiveBetween(1, 50);

        RuleFor(request => request.Theme)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.LevelRange.Min)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.LevelRange)
            .Must(range => range.Min <= range.Max)
            .WithMessage("levelRange.min must be less than or equal to levelRange.max.");
    }
}
