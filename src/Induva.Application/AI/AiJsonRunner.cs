using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Induva.Application.Abstractions;
using Induva.Application.Common;
using Induva.Domain.Entities;
using Induva.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Induva.Application.AI;

public sealed class AiJsonRunner(
    IAiClient client,
    IUnitOfWork uow,
    IOptions<AiOptions> opts,
    ILogger<AiJsonRunner> log) : IAiJsonRunner
{
    public async Task<Result<AiOutput<T>>> RunAsync<T>(AiJsonCall call, CancellationToken ct = default) where T : class
    {
        var o = opts.Value;
        var runs = uow.Repository<AiAnalysisRun>();

        // 1) per-user daily quota (cache hits do not count towards quota)
        var today = DateTime.UtcNow.Date;
        var countToday = await runs.CountAsync(r => r.UserId == call.UserId && r.CreatedAtUtc >= today, ct);
        if (countToday >= o.DailyLimitPerUser)
        {
            return Result<AiOutput<T>>.Fail(ErrorKind.RateLimited, "You reached today's AI limit. Please try again tomorrow.");
        }

        // 2) cache lookup by input hash
        var hash = Sha256($"{call.Feature}|{call.PromptVersion}|{call.SystemPrompt}|{call.UserPrompt}");
        if (call.UseCache)
        {
            var since = DateTime.UtcNow.AddHours(-o.CacheHours);
            var hits = await runs.ListAsync(
                r => r.InputHash == hash && r.Status == AiRunStatus.Succeeded && r.CreatedAtUtc > since, ct);

            var hit = hits.OrderByDescending(r => r.CreatedAtUtc).FirstOrDefault();
            if (hit is not null)
            {
                var cached = JsonExtractor.TryDeserialize<T>(hit.ParsedJson);
                if (cached is not null)
                {
                    return Result<AiOutput<T>>.Ok(new AiOutput<T>(cached, hit.Id, FromCache: true));
                }
            }
        }

        // 3) call AI (+ one repair retry when JSON is unreadable)
        var sw = Stopwatch.StartNew();
        string? raw = null;
        T? parsed = null;
        string? error = null;
        var kind = ErrorKind.AiUnavailable;

        try
        {
            var req = new AiRequest(call.Feature, call.SystemPrompt, call.UserPrompt);
            var resp = await client.ChatAsync(req, ct);
            raw = resp.Text;
            parsed = JsonExtractor.TryDeserialize<T>(raw);

            if (parsed is null)
            {
                log.LogInformation("First attempt for {Feature} returned invalid JSON, retrying once with repair prompt...", call.Feature);
                var repair = req with
                {
                    UserPrompt = call.UserPrompt + "\n\nYour previous reply was not valid JSON. Reply again with ONLY the JSON object, nothing else."
                };
                resp = await client.ChatAsync(repair, ct);
                raw = resp.Text;
                parsed = JsonExtractor.TryDeserialize<T>(raw);
            }

            if (parsed is null)
            {
                error = "The AI returned an unreadable answer. Please try again.";
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            error = "The AI took too long to answer. Please try again.";
        }
        catch (AiProviderException ex)
        {
            log.LogWarning("AI provider returned HTTP {StatusCode} for feature {Feature}", ex.StatusCode, call.Feature);
            kind = ex.StatusCode == 429 ? ErrorKind.RateLimited : ErrorKind.AiUnavailable;
            error = kind == ErrorKind.RateLimited
                ? "The AI service is busy. Please retry in a minute."
                : "The AI service is currently unavailable.";
        }
        catch (HttpRequestException ex)
        {
            log.LogWarning(ex, "AI network failure for feature {Feature}", call.Feature);
            error = "Could not reach the AI service.";
        }
        sw.Stop();

        // 4) persist audit row
        var run = new AiAnalysisRun
        {
            Feature = call.Feature,
            UserId = call.UserId,
            EntityType = call.EntityType,
            EntityId = call.EntityId,
            PromptVersion = call.PromptVersion,
            Model = o.ModelId,
            InputHash = hash,
            RawResponse = raw is { Length: > 8000 } ? raw[..8000] : raw,
            ParsedJson = parsed is null ? null : JsonSerializer.Serialize(parsed, JsonExtractor.Options),
            Status = parsed is null ? AiRunStatus.Failed : AiRunStatus.Succeeded,
            ErrorMessage = error,
            LatencyMs = (int)sw.ElapsedMilliseconds
        };

        await runs.AddAsync(run, ct);
        await uow.SaveChangesAsync(ct);

        return parsed is null
            ? Result<AiOutput<T>>.Fail(kind, error ?? "AI analysis failed.")
            : Result<AiOutput<T>>.Ok(new AiOutput<T>(parsed, run.Id, FromCache: false));
    }

    private static string Sha256(string s) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}

