using FluentAssertions;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Platform.Messaging.Contracts;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Contracts;

[EventName("test.event.registered", Version = 1)]
file sealed record TestRegisteredIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid? SourceEventId { get; init; }
    public string MessageName => "test.event.registered";
    public int SchemaVersion => 1;
    public Guid? AccountId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class EventDescriptorProviderTests
{
    private readonly EventDescriptorProvider _sut = new();

    [Fact]
    public void Get_ByType_ShouldReturnDescriptor()
    {
        var descriptor = _sut.Get(typeof(TestRegisteredIntegrationEvent));

        descriptor.Should().NotBeNull();
        descriptor.Name.Should().Be("test.event.registered");
        descriptor.Version.Should().Be(1);
        descriptor.EventType.Should().Be(typeof(TestRegisteredIntegrationEvent));
    }

    [Fact]
    public void Get_ByNameAndVersion_ShouldReturnDescriptor()
    {
        var descriptor = _sut.Get("test.event.registered", 1);

        descriptor.Should().NotBeNull();
        descriptor.EventType.Should().Be(typeof(TestRegisteredIntegrationEvent));
    }

    [Fact]
    public void Get_ByType_ShouldThrowForUnknownType()
    {
        var act = () => _sut.Get(typeof(string));

        act.Should().Throw<UnknownEventDescriptorException>()
            .WithMessage("*string*");
    }

    [Fact]
    public void Get_ByNameAndVersion_ShouldThrowForUnknownEvent()
    {
        var act = () => _sut.Get("nonexistent.event", 1);

        act.Should().Throw<UnknownEventDescriptorException>()
            .WithMessage("*nonexistent.event*");
    }

    [Fact]
    public void Get_ByTypeAndByName_ShouldReturnSameDescriptor()
    {
        var byType = _sut.Get(typeof(TestRegisteredIntegrationEvent));
        var byName = _sut.Get("test.event.registered", 1);

        byType.Should().BeEquivalentTo(byName);
    }

    [Fact]
    public void Get_ByType_ShouldReturnDescriptorForExistingApplicationEvent()
    {
        var descriptor = _sut.Get(typeof(Notrelix.Application.Events.WorkManagement.BoardCreatedIntegrationEvent));

        descriptor.Should().NotBeNull();
        descriptor.Name.Should().Be("board.created");
    }
}
