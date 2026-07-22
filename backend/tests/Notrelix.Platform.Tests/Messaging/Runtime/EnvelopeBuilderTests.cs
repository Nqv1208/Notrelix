using System.Text;
using FluentAssertions;
using Moq;
using Notrelix.Application.Common.Events;
using Notrelix.Domain.Common;
using Notrelix.Platform.Messaging.Contracts;
using Notrelix.Platform.Messaging.Runtime;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Runtime;

[EventName("test.built", Version = 1)]
file sealed record TestBuildEvent : IIntegrationEvent
{
    public Guid EventId { get; init; }
    public Guid? SourceEventId { get; init; }
    public string MessageName => "test.built";
    public int SchemaVersion => 1;
    public Guid? AccountId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid CorrelationId { get; init; }
    public Guid? CausationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class EnvelopeBuilderTests
{
    private readonly Mock<IEventDescriptorProvider> _providerMock = new();
    private readonly Mock<IEventSerializer> _serializerMock = new();
    private readonly EnvelopeBuilder _sut;

    public EnvelopeBuilderTests()
    {
        _sut = new EnvelopeBuilder(_providerMock.Object, _serializerMock.Object);
    }

    [Fact]
    public void Build_ShouldPopulateAllFields()
    {
        var correlationId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;

        _providerMock.Setup(p => p.Get(typeof(TestBuildEvent)))
            .Returns(new EventDescriptor
            {
                Name = "test.built",
                Version = 1,
                EventType = typeof(TestBuildEvent),
                Classification = EventClassification.Business,
            });

        _serializerMock.Setup(s => s.Serialize(It.IsAny<object>(), typeof(TestBuildEvent)))
            .Returns(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("{}")));

        var publication = new EventPublication
        {
            Event = new TestBuildEvent
            {
                EventId = Guid.NewGuid(),
                CorrelationId = correlationId,
                OccurredAt = occurredAt,
            },
            Context = new PublishContext
            {
                CorrelationId = correlationId,
                WorkspaceId = workspaceId,
                OccurredAt = occurredAt,
            },
        };

        var envelope = _sut.Build(publication);

        envelope.EventName.Should().Be("test.built");
        envelope.EventVersion.Should().Be(1);
        envelope.CorrelationId.Should().Be(correlationId);
        envelope.WorkspaceId.Should().Be(workspaceId);
        envelope.OccurredAt.Should().Be(occurredAt);
        envelope.ContentType.Should().Be("application/json");
        envelope.Classification.Should().Be(EventClassification.Business);
        envelope.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Build_ShouldSerializeEvent()
    {
        var evt = new TestBuildEvent
        {
            EventId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
        };

        _providerMock.Setup(p => p.Get(typeof(TestBuildEvent)))
            .Returns(new EventDescriptor
            {
                Name = "test.built",
                Version = 1,
                EventType = typeof(TestBuildEvent),
            });

        _serializerMock.Setup(s => s.Serialize(evt, typeof(TestBuildEvent)))
            .Returns(new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes("{\"key\":\"value\"}")));

        var publication = new EventPublication
        {
            Event = evt,
            Context = new PublishContext
            {
                CorrelationId = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
            },
        };

        var envelope = _sut.Build(publication);

        Encoding.UTF8.GetString(envelope.Data.Span).Should().Be("{\"key\":\"value\"}");
    }
}
