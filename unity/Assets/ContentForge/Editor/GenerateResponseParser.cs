using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ContentForge.Editor
{
    /// <summary>Raised when a server response cannot be parsed into typed content.</summary>
    internal sealed class GenerateResponseParseException : Exception
    {
        public GenerateResponseParseException(string message) : base(message)
        {
        }
    }

    /// <summary>Parses a <c>POST /api/v1/generate</c> success body into typed DTOs.
    /// Pure — no Unity/editor dependencies, so it is fully unit-testable.</summary>
    internal static class GenerateResponseParser
    {
        public static ParsedGeneration Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GenerateResponseParseException("Empty response body.");
            }

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new GenerateResponseParseException($"Response was not valid JSON: {ex.Message}");
            }

            var typeToken = root["contentType"]?.ToString();
            if (string.IsNullOrWhiteSpace(typeToken))
            {
                throw new GenerateResponseParseException("Response is missing 'contentType'.");
            }

            if (!Enum.TryParse<GeneratedContentType>(typeToken, ignoreCase: true, out var contentType)
                || !Enum.IsDefined(typeof(GeneratedContentType), contentType))
            {
                throw new GenerateResponseParseException($"Unknown content type '{typeToken}'.");
            }

            if (root["content"] is not JArray content)
            {
                throw new GenerateResponseParseException("Response is missing a 'content' array.");
            }

            if (content.Count == 0)
            {
                throw new GenerateResponseParseException("Response 'content' array is empty.");
            }

            var result = new ParsedGeneration { ContentType = contentType };
            try
            {
                if (contentType == GeneratedContentType.Item)
                {
                    result.Items = content.ToObject<List<GeneratedItemDto>>();
                }
                else
                {
                    result.Enemies = content.ToObject<List<GeneratedEnemyDto>>();
                }
            }
            catch (JsonException ex)
            {
                throw new GenerateResponseParseException($"Could not read '{contentType}' entries: {ex.Message}");
            }

            return result;
        }
    }
}
