using System.Text.Json;
using System.Text.RegularExpressions;

namespace LabTrack.Infrastructure.Ai;

public class AiResponseValidator
{
    private static readonly Regex JsonExtractorRegex = new(
        @"```(?:json)?\s*(.*?)\s*```",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    public bool IsValidTextResponse(string? response, out string cleanedResponse)
    {
        cleanedResponse = string.Empty;

        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        cleanedResponse = response.Trim();
        return cleanedResponse.Length > 0;
    }

    public bool TryExtractJson(string response, out string jsonContent)
    {
        jsonContent = string.Empty;

        if (string.IsNullOrWhiteSpace(response))
        {
            return false;
        }

        var match = JsonExtractorRegex.Match(response);
        if (match.Success)
        {
            jsonContent = match.Groups[1].Value.Trim();
            return true;
        }

        var trimmed = response.Trim();
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            jsonContent = trimmed;
            return true;
        }

        return false;
    }

    public bool TryDeserializeJson<T>(string response, out T? result, JsonSerializerOptions? options = null)
    {
        result = default;

        if (!TryExtractJson(response, out var jsonContent))
        {
            return false;
        }

        try
        {
            options ??= new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            result = JsonSerializer.Deserialize<T>(jsonContent, options);
            return result is not null;
        }
        catch
        {
            return false;
        }
    }
}