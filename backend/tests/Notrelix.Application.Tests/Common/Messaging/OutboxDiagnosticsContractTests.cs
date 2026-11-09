using Notrelix.Application.Common.Messaging;

namespace Notrelix.Application.Tests.Common.Messaging;

public class OutboxDiagnosticsContractTests
{
    [Theory]
    [InlineData(5000, 200)]
    [InlineData(201, 200)]
    [InlineData(200, 200)]
    [InlineData(50, 50)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    public void Clamp_KeepsEveryOperatorReadWithinOneToTwoHundredRows(int requested, int expected)
    {
        OutboxDiagnosticsLimits.Clamp(requested).Should().Be(expected);
    }

    [Fact]
    public void DetailResult_CarriesNoPayloadField()
    {
        var names = typeof(OutboxMessageDetailResult).GetProperties().Select(p => p.Name).ToList();

        names.Should().NotContain(n => n.Contains("Payload", StringComparison.OrdinalIgnoreCase),
            "operator reads return no payload (Lead decision L9)");
    }

    [Fact]
    public void DetailResult_SerializesWithoutAnyPayloadKey()
    {
        var detail = new OutboxMessageDetailResult(
            Guid.CreateVersion7(), Guid.CreateVersion7(), null, "Board.Created", 1, "ctx.source", null,
            "Failed", 3, 5, DateTimeOffset.UtcNow, null, null, null, "boom", null, null, null, null);

        var json = System.Text.Json.JsonSerializer.Serialize(detail);

        json.ToLowerInvariant().Should().NotContain("payload");
    }
}
