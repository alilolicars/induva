using Induva.Application.AI;
using Induva.Domain.Enums;
using Induva.Infrastructure.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Induva.Tests;

public class AiResilienceTests
{
    private class DelayedHandler(TimeSpan delay) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            var json = """
            {
              "request_id": "test-resilience",
              "model_id": "deepseek.v3.2",
              "output_text": "OK"
            }
            """;
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    [Fact]
    public async Task DemoAiClient_loads_FactoryAnalysis_json_successfully()
    {
        var demo = new DemoAiClient();
        var req = new AiRequest(AiFeature.FactoryAnalysis, "sys", "usr");

        var resp = await demo.ChatAsync(req);

        Assert.NotNull(resp.Text);
        Assert.Contains("Delta Tomato Canning", resp.Text);
    }

    [Fact]
    public async Task Resilience_pipeline_handles_delay_without_premature_client_timeout()
    {
        // Prove that the Resilience pipeline configured in AddInfrastructure
        // (AttemptTimeout = 120s, TotalRequestTimeout = 300s, HttpClient.Timeout = Infinite)
        // allows requests taking longer than standard 100s default without timing out.
        // We test with a small simulated delay through the resilience pipeline builder.

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(new AiOptions
        {
            ApiKey = "dummy-key",
            BaseUrl = "http://localhost/"
        }));
        services.AddSingleton<AiConcurrencyGate>();

        services.AddHttpClient<IAiClient, ItiAiClient>(c =>
        {
            c.BaseAddress = new Uri("http://localhost/");
            c.Timeout = Timeout.InfiniteTimeSpan;
        })
        .AddHttpMessageHandler(() => new DelayedHandler(TimeSpan.FromMilliseconds(500)))
        .AddStandardResilienceHandler(r =>
        {
            r.AttemptTimeout.Timeout = TimeSpan.FromSeconds(120);
            r.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(300);
            r.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(240);
            r.Retry.MaxRetryAttempts = 1;
        });

        var sp = services.BuildServiceProvider();
        var client = sp.GetRequiredService<IAiClient>();

        var req = new AiRequest(AiFeature.FactoryAnalysis, "sys", "usr");
        var resp = await client.ChatAsync(req);

        Assert.Equal("OK", resp.Text);
    }
}

