using FluentAssertions;
using Notrelix.Application.Common.Events;
using Notrelix.Platform.Messaging.Runtime;
using Notrelix.Platform.Messaging.Transport;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Transport;

public sealed class InMemoryTransportAdapterTests
{
    private readonly InMemoryTransportAdapter _sut = new();

    [Fact]
    public async Task SendAsync_ShouldStoreEnvelope()
    {
        var envelope = CreateEnvelope();

        await _sut.SendAsync(envelope);

        _sut.Published.Should().ContainSingle();
        _sut.Published[0].EventName.Should().Be("test.transport");
    }

    [Fact]
    public async Task SendAsync_ShouldStoreMultipleEnvelopes()
    {
        await _sut.SendAsync(CreateEnvelope("evt.1"));
        await _sut.SendAsync(CreateEnvelope("evt.2"));

        _sut.Published.Should().HaveCount(2);
    }

    [Fact]
    public void IsConnected_ShouldBeTrue_ByDefault()
    {
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectAndDisconnect_ShouldToggleState()
    {
        await _sut.DisconnectAsync();
        _sut.IsConnected.Should().BeFalse();

        await _sut.ConnectAsync();
        _sut.IsConnected.Should().BeTrue();
    }

    [Fact]
    public void Clear_ShouldRemoveAllEnvelopes()
    {
        _sut.SendAsync(CreateEnvelope());
        _sut.Clear();

        _sut.Published.Should().BeEmpty();
    }

    private static EventEnvelope CreateEnvelope(string eventName = "test.transport") => new()
    {
        EventName = eventName,
        EventVersion = 1,
        CorrelationId = Guid.NewGuid(),
        OccurredAt = DateTimeOffset.UtcNow,
        Data = Array.Empty<byte>(),
        ContentType = "application/json",
    };
}
