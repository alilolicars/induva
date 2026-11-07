using Induva.Application.AI;
using Xunit;

namespace Induva.Tests;

public class JsonExtractorTests
{
    private record SampleDto(string Name, int Score);

    [Fact]
    public void Extracts_json_from_markdown_fence()
    {
        var text = "Here is the response:\n```json\n{\"name\":\"Egyptian Cotton Co.\",\"score\":95}\n```\nHope that helps!";
        var result = JsonExtractor.TryDeserialize<SampleDto>(text);

        Assert.NotNull(result);
        Assert.Equal("Egyptian Cotton Co.", result.Name);
        Assert.Equal(95, result.Score);
    }

    [Fact]
    public void Strips_think_blocks()
    {
        var text = "<think>Let me reason about the score: score should be 88, name is Alpha</think>{\"name\":\"Alpha\",\"score\":88}";
        var result = JsonExtractor.TryDeserialize<SampleDto>(text);

        Assert.NotNull(result);
        Assert.Equal("Alpha", result.Name);
        Assert.Equal(88, result.Score);
    }

    [Fact]
    public void Returns_null_for_garbage()
    {
        var text = "I am sorry, as an AI language model I cannot answer this request.";
        var result = JsonExtractor.TryDeserialize<SampleDto>(text);

        Assert.Null(result);
    }

    [Fact]
    public void Accepts_numbers_as_strings()
    {
        var text = "{\"name\":\"Plastic Molding\",\"score\":\"42\"}";
        var result = JsonExtractor.TryDeserialize<SampleDto>(text);

        Assert.NotNull(result);
        Assert.Equal("Plastic Molding", result.Name);
        Assert.Equal(42, result.Score);
    }
}

