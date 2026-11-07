using Induva.Application.Common;
using Induva.Domain.Enums;

namespace Induva.Application.AI;

public sealed record AiJsonCall(
    AiFeature Feature,
    string UserId,
    string EntityType,
    int EntityId,
    string SystemPrompt,
    string UserPrompt,
    string PromptVersion,
    bool UseCache = true);

public sealed record AiOutput<T>(T Data, int RunId, bool FromCache);

public interface IAiJsonRunner
{
    Task<Result<AiOutput<T>>> RunAsync<T>(AiJsonCall call, CancellationToken ct = default) where T : class;
}

