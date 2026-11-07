using Induva.Infrastructure.AI;
using Xunit;

namespace Induva.Tests;

public class ItiResponseParserTests
{
    [Fact]
    public void Extracts_output_text_from_live_iti_response_shape()
    {
        var rawM0Sample = """
        {
          "request_id": "c704dd29-a1b5-480d-baaa-f74ff463a77c",
          "model_id": "deepseek.v3.2",
          "region": "us-east-2",
          "output_text": "{\"summary\":\"Factory is capable of injection molding.\"}",
          "usage": { "input_tokens": 17, "output_tokens": 2, "total_tokens": 19 }
        }
        """;

        var text = ItiResponseParser.ExtractText(rawM0Sample);

        Assert.Equal("{\"summary\":\"Factory is capable of injection molding.\"}", text);
    }

    [Fact]
    public void Extracts_from_openai_choices_format()
    {
        var rawOpenAi = """
        {
          "id": "chatcmpl-123",
          "choices": [
            {
              "message": {
                "role": "assistant",
                "content": "{\"summary\":\"Textile manufacturing\"}"
              }
            }
          ]
        }
        """;

        var text = ItiResponseParser.ExtractText(rawOpenAi);

        Assert.Equal("{\"summary\":\"Textile manufacturing\"}", text);
    }

    [Fact]
    public void Falls_back_to_plain_text_when_not_json()
    {
        var plain = "Raw plain text answer from an unexpected proxy";
        var text = ItiResponseParser.ExtractText(plain);

        Assert.Equal(plain, text);
    }
}

