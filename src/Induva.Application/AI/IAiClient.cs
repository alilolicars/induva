using Induva.Domain.Enums;

namespace Induva.Application.AI;

public sealed record AiRequest(AiFeature Feature, string SystemPrompt, string UserPrompt);

public sealed record AiResponse(string Text, string RawBody);

public interface IAiClient
{
    Task<AiResponse> ChatAsync(AiRequest request, CancellationToken ct = default);
}

public sealed class AiProviderException(int statusCode, string body)
    : Exception($"AI provider returned status code {statusCode}")
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
}

