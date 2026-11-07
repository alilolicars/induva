using Induva.Application.AI;

namespace Induva.Infrastructure.AI;

public sealed class DemoAiClient : IAiClient
{
    public async Task<AiResponse> ChatAsync(AiRequest req, CancellationToken ct = default)
    {
        await Task.Delay(1200, ct); // simulate network & inference latency

        var path = Path.Combine(AppContext.BaseDirectory, "DemoResponses", $"{req.Feature}.json");
        if (!File.Exists(path))
        {
            throw new AiProviderException(503, $"No demo response found for feature {req.Feature}");
        }

        var json = await File.ReadAllTextAsync(path, ct);
        return new AiResponse(json, json);
    }
}

