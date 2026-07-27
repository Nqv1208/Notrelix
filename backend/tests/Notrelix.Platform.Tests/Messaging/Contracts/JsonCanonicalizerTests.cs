using System.Text;
using System.Text.Json;
using FluentAssertions;
using Notrelix.Platform.Messaging.Contracts;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Contracts;

public sealed class JsonCanonicalizerTests
{
    private readonly JsonCanonicalizer _sut = new();

    [Fact]
    public void Canonicalize_ShouldNormalizeJson()
    {
        var input = """{"b":2,"a":1}""";
        var data = Encoding.UTF8.GetBytes(input);

        var result = _sut.Canonicalize(data);

        var output = JsonSerializer.Deserialize<Dictionary<string, int>>(result.Span);
        output.Should().ContainKey("a");
        output.Should().ContainKey("b");
    }

    [Fact]
    public void Canonicalize_ShouldStripWhitespace()
    {
        var input = """
        {
            "name": "test",
            "value": 42
        }
        """;
        var data = Encoding.UTF8.GetBytes(input);

        var result = _sut.Canonicalize(data);

        var output = Encoding.UTF8.GetString(result.Span);
        output.Should().NotContain(" ");
        output.Should().NotContain("\n");
    }

    [Fact]
    public void Canonicalize_ShouldHandleEmptyObject()
    {
        ReadOnlyMemory<byte> data = "{}"u8.ToArray();

        var result = _sut.Canonicalize(data);

        var output = Encoding.UTF8.GetString(result.Span);
        output.Should().Be("{}");
    }

    [Fact]
    public void Canonicalize_ShouldBeIdempotent()
    {
        var input = """{"b":[3,2,1],"a":{"z":1,"y":2}}""";
        var data = Encoding.UTF8.GetBytes(input);

        var first = _sut.Canonicalize(data);
        var second = _sut.Canonicalize(data);

        first.Span.SequenceEqual(second.Span).Should().BeTrue();
    }
}
