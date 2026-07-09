namespace ContentForge.Api.Generation;

/// <summary>Successful response for <c>POST /api/v1/generate</c>.</summary>
/// <param name="ContentType">The kind of content that was generated.</param>
/// <param name="Content">Validated entries — <see cref="GeneratedItem"/> or <see cref="GeneratedEnemy"/>.</param>
public sealed record GenerateResponse(ContentType ContentType, IReadOnlyList<object> Content);
