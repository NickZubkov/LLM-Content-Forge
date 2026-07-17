namespace ContentForge.Api.Generation;

/// <summary>Successful response for <c>POST /api/v1/generate</c>.</summary>
/// <param name="ContentType">The kind of content that was generated.</param>
/// <param name="Content">
/// Validated entries — all <see cref="GeneratedItem"/> or all <see cref="GeneratedEnemy"/>,
/// matching <paramref name="ContentType"/>. Typed as <c>object</c> on purpose: the two shapes
/// share no fields, System.Text.Json serializes each element by its runtime type, and the Unity
/// client re-parses by <c>contentType</c> anyway. Separate typed fields (items/enemies) are the
/// planned move if more content types appear.
/// </param>
public sealed record GenerateResponse(ContentType ContentType, IReadOnlyList<object> Content);
