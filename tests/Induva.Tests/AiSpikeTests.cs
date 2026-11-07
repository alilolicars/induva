using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Induva.Tests;

public class AiSpikeTests(ITestOutputHelper output)
{
    private const string BaseUrl = "http://apiaccess.iti.net.eg/api/v1/";
    private const string ModelId = "deepseek.v3.2";

    private static string GetApiKey()
    {
        var env = Environment.GetEnvironmentVariable("SBG_API_KEY");
        if (!string.IsNullOrWhiteSpace(env)) return env;

        var desktopKey = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "API_KEY.txt");
        if (File.Exists(desktopKey)) return File.ReadAllText(desktopKey).Trim();

        return "";
    }

    private static string GetDocsDir()
    {
        // Find C:\PROJECT\DOCS or relative to solution
        var candidate = @"C:\PROJECT\DOCS";
        if (Directory.Exists(candidate)) return candidate;
        candidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DOCS"));
        if (Directory.Exists(candidate)) return candidate;
        Directory.CreateDirectory(candidate);
        return candidate;
    }

    [Fact]
    public async Task Run_M0_Spike_And_Record_Findings()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            output.WriteLine("Skipping live spike: SBG_API_KEY / API_KEY.txt not set.");
            return;
        }

        using var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.Timeout = TimeSpan.FromSeconds(120);

        // 1. Trivial prompt
        output.WriteLine("=== SPIKE 1: Trivial Prompt ===");
        var payload1 = new
        {
            model_id = ModelId,
            system_prompt = "You are a test assistant.",
            messages = new[] { new { role = "user", content = "Reply with the single word OK." } }
        };

        var sw = Stopwatch.StartNew();
        using var res1 = await client.PostAsJsonAsync("student/chat", payload1);
        sw.Stop();

        var body1 = await res1.Content.ReadAsStringAsync();
        output.WriteLine($"Status: {res1.StatusCode}");
        output.WriteLine($"Latency: {sw.ElapsedMilliseconds} ms");
        output.WriteLine("Headers:");
        foreach (var h in res1.Headers)
            output.WriteLine($"  {h.Key}: {string.Join(", ", h.Value)}");
        output.WriteLine($"Raw Body:\n{body1}\n");

        var samplePath = Path.Combine(GetDocsDir(), "ai-response-sample.json");
        await File.WriteAllTextAsync(samplePath, body1);
        output.WriteLine($"Saved sample to {Path.GetFullPath(samplePath)}");

        // 2. Structured JSON prompt (testing <think> blocks, markdown fences, JSON shape)
        output.WriteLine("\n=== SPIKE 2: Structured JSON Prompt (~1.5k tokens) ===");
        var payload2 = new
        {
            model_id = ModelId,
            system_prompt = "You are INDUVA's industrial analyst. Reply with ONE JSON object. No markdown fences, no text before or after.",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = @"Analyze this factory and output JSON matching schema:
{
  ""summary"": ""string"",
  ""capabilityTags"": [""string""],
  ""processes"": [ { ""name"": ""string"", ""machines"": [""string""], ""materialsProcessable"": [""string""] } ],
  ""strengths"": [""string""],
  ""constraints"": [""string""],
  ""confidence"": 0.85
}
Factory data:
Name: Delta Tomato Canning Co.
Sector: FoodProcessing
Governorate: Sharqia
Machines: washing line, hot-break pulper, vacuum evaporator, can seamer, retort sterilizer, glass-jar filling line
Current products: tomato paste, canned tomatoes"
                }
            }
        };

        sw.Restart();
        using var res2 = await client.PostAsJsonAsync("student/chat", payload2);
        sw.Stop();

        var body2 = await res2.Content.ReadAsStringAsync();
        output.WriteLine($"Status: {res2.StatusCode}");
        output.WriteLine($"Latency: {sw.ElapsedMilliseconds} ms");
        output.WriteLine($"Has <think> block: {body2.Contains("<think>")}");
        output.WriteLine($"Has ```json fence: {body2.Contains("```json") || body2.Contains("```")}");
        output.WriteLine($"Raw Body 2:\n{body2}\n");

        // Also save the structured response as sample2
        var sample2Path = Path.Combine(GetDocsDir(), "ai-response-structured-sample.json");
        await File.WriteAllTextAsync(sample2Path, body2);

        // 3. Test temperature & max_tokens acceptance
        output.WriteLine("\n=== SPIKE 3: Testing temperature and max_tokens fields ===");
        var payload3 = new
        {
            model_id = ModelId,
            system_prompt = "You are a test assistant.",
            messages = new[] { new { role = "user", content = "Reply with OK." } },
            temperature = 0.2,
            max_tokens = 500
        };

        sw.Restart();
        using var res3 = await client.PostAsJsonAsync("student/chat", payload3);
        sw.Stop();

        var body3 = await res3.Content.ReadAsStringAsync();
        output.WriteLine($"Status with temperature/max_tokens: {res3.StatusCode}");
        output.WriteLine($"Raw Body 3:\n{body3}");
    }
}
