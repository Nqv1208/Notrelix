using FluentAssertions;
using Notrelix.Application.Common.Events;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Transport;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Transport;

public sealed class NullTransportAdapterTests
{
    [Fact]
    public async Task SendAsync_ShouldSucceed_InDevelopmentMode()
    {
        var sut = new NullTransportAdapter(development: true);
        var envelope = CreateEnvelope();

        var result = await sut.SendAsync(envelope);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public void SendAsync_ShouldThrow_WhenNotDevelopment()
    {
        var sut = new NullTransportAdapter(development: false);
        var envelope = CreateEnvelope();

        var act = () => sut.SendAsync(envelope);

        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*development*");
    }

    [Fact]
    public async Task ConnectAndDisconnect_ShouldToggleState()
    {
        var sut = new NullTransportAdapter();
        sut.IsConnected.Should().BeFalse();

        await sut.ConnectAsync();
        sut.IsConnected.Should().BeTrue();

        await sut.DisconnectAsync();
        sut.IsConnected.Should().BeFalse();
    }

    private static EventEnvelope CreateEnvelope() => new()
    {
        EventName = "test.null",
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = Array.Empty<byte>(),
        ContentType = "application/json",
    };
}
