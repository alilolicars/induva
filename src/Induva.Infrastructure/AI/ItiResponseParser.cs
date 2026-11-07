using System.Text.Json;

namespace Induva.Infrastructure.AI;

public static class ItiResponseParser
{
    private static readonly string[] TextKeys =
    [
        "output_text", // Discovered from M0 live spike on ITI API (model deepseek.v3.2)
        "response",
        "content",
        "text",
        "output",
        "answer",
        "reply",
        "message"
    ];

    public static string ExtractText(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var key in TextKeys)
                {
                    if (root.TryGetProperty(key, out var v) && Flatten(v) is { Length: > 0 } s)
                        return s;
                }

                if (root.TryGetProperty("choices", out var ch) &&
                    ch.ValueKind == JsonValueKind.Array &&
                    ch.GetArrayLength() > 0 &&
                    ch[0].TryGetProperty("message", out var m) &&
                    m.TryGetProperty("content", out var c) &&
                    Flatten(c) is { Length: > 0 } s2)
                {
                    return s2;
                }

                if (root.TryGetProperty("data", out var d))
                    return ExtractText(d.GetRawText());
            }
        }
        catch (JsonException)
        {
            // Body is plain text
        }

        return body;
    }

    private static string? Flatten(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString(),
        JsonValueKind.Object when v.TryGetProperty("content", out var c) => Flatten(c),
        _ => null
    };
}

