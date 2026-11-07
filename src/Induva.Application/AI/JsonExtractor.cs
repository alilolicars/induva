using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Induva.Application.AI;

public static class JsonExtractor
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Removes &lt;think&gt; blocks and markdown fences, returns the outermost {...} or null.
    /// </summary>
    public static string? ExtractJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        text = Regex.Replace(text, "<think>.*?</think>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = text.Replace("```json", "", StringComparison.OrdinalIgnoreCase).Replace("```", "");

        int s = text.IndexOf('{');
        int e = text.LastIndexOf('}');
        return s >= 0 && e > s ? text[s..(e + 1)] : null;
    }

    public static T? TryDeserialize<T>(string? text) where T : class
    {
        try
        {
            var j = ExtractJson(text);
            return j is null ? null : JsonSerializer.Deserialize<T>(j, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

