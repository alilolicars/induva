using System.Net.Http.Json;
using Induva.Application.AI;
using Microsoft.Extensions.Options;

namespace Induva.Infrastructure.AI;

public sealed class AiConcurrencyGate(IOptions<AiOptions> options)
{
    public SemaphoreSlim Semaphore { get; } = new(Math.Max(1, options.Value.MaxConcurrentCalls));
}

public sealed class ItiAiClient(
    HttpClient http,
    IOptions<AiOptions> opts,
    AiConcurrencyGate gate) : IAiClient
{
    public async Task<AiResponse> ChatAsync(AiRequest req, CancellationToken ct = default)
    {
        var payload = new
        {
            model_id = opts.Value.ModelId,
            system_prompt = req.SystemPrompt,
            messages = new[]
            {
                new { role = "user", content = req.UserPrompt }
            },
            temperature = 0.2 // Spike confirmed this parameter is accepted
        };

        await gate.Semaphore.WaitAsync(ct);
        try
        {
            using var res = await http.PostAsJsonAsync("student/chat", payload, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                throw new AiProviderException((int)res.StatusCode, body);
            }

            return new AiResponse(ItiResponseParser.ExtractText(body), body);
        }
        finally
        {
            gate.Semaphore.Release();
        }
    }
}

