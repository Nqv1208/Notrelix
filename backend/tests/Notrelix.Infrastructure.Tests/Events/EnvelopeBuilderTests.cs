using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Serialization;

namespace Notrelix.Infrastructure.Tests.Events;

public class EnvelopeBuilderTests
{
    private readonly Mock<IEventSerializer> _serializer = new();
    private readonly Mock<IContractRegistry> _contractRegistry = new();

    private readonly EnvelopeContext _context = new()
    {
        EventName = "OrderCreated",
        EventVersion = 2,
        SourceContext = "Sales",
        AggregateType = "Order",
        AggregateId = Guid.NewGuid(),
        SubjectType = "User",
        SubjectId = Guid.NewGuid(),
        WorkspaceId = Guid.NewGuid(),
        ActorUserId = Guid.NewGuid(),
        CorrelationId = "corr-123",
        CausationId = "cause-456",
        TraceParent = "00-abc-def-01",
        TraceState = "state=value",
        OccurredAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero),
        PartitionKey = "part-1",
        TenantId = "tenant-1",
    };

    [Fact]
    public void Build_PopulatesAllFields()
    {
        var @event = new FakeIntegrationEvent();
        SetupSerializerAndRegistry();

        var builder = new EnvelopeBuilder(_serializer.Object, _contractRegistry.Object);
        var envelope = builder.Build(@event, _context);

        envelope.EventName.Should().Be(_context.EventName);
        envelope.EventVersion.Should().Be(_context.EventVersion);
        envelope.SourceContext.Should().Be(_context.SourceContext);
        envelope.AggregateType.Should().Be(_context.AggregateType);
        envelope.AggregateId.Should().Be(_context.AggregateId);
        envelope.SubjectType.Should().Be(_context.SubjectType);
        envelope.SubjectId.Should().Be(_context.SubjectId);
        envelope.WorkspaceId.Should().Be(_context.WorkspaceId);
        envelope.ActorUserId.Should().Be(_context.ActorUserId);
        envelope.CorrelationId.Should().Be(_context.CorrelationId);
        envelope.CausationId.Should().Be(_context.CausationId);
        envelope.TraceParent.Should().Be(_context.TraceParent);
        envelope.TraceState.Should().Be(_context.TraceState);
        envelope.OccurredAt.Should().Be(_context.OccurredAt);
        envelope.PartitionKey.Should().Be(_context.PartitionKey);
        envelope.TenantId.Should().Be(_context.TenantId);
    }

    [Fact]
    public void Build_UsesSerializer()
    {
        var @event = new FakeIntegrationEvent();
        var serialized = new ReadOnlyMemory<byte>([1, 2, 3]);
        _serializer
            .Setup(s => s.Serialize(It.IsAny<IIntegrationEvent>()))
            .Returns(serialized);
        _contractRegistry
            .Setup(r => r.GetByType(typeof(FakeIntegrationEvent)))
            .Returns(new ContractDefinition { Name = "FakeIntegrationEvent", Version = 1 });

        var builder = new EnvelopeBuilder(_serializer.Object, _contractRegistry.Object);
        var envelope = builder.Build(@event, _context);

        _serializer.Verify(
            s => s.Serialize(It.IsAny<IIntegrationEvent>()), Times.Once);
        envelope.Data.ToArray().Should().Equal(serialized.ToArray());
    }

    [Fact]
    public void Build_UsesContractRegistry()
    {
        var @event = new FakeIntegrationEvent();
        SetupSerializerAndRegistry(EventClassification.Audit);

        var builder = new EnvelopeBuilder(_serializer.Object, _contractRegistry.Object);
        var envelope = builder.Build(@event, _context);

        _contractRegistry.Verify(r => r.GetByType(@event.GetType()), Times.Once);
        envelope.Classification.Should().Be(EventClassification.Audit);
    }

    [Fact]
    public void Build_GeneratesId()
    {
        var @event = new FakeIntegrationEvent();
        SetupSerializerAndRegistry();

        var builder = new EnvelopeBuilder(_serializer.Object, _contractRegistry.Object);
        var envelope = builder.Build(@event, _context);

        envelope.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Build_SetsCorrectContentType()
    {
        var @event = new FakeIntegrationEvent();
        SetupSerializerAndRegistry();

        var builder = new EnvelopeBuilder(_serializer.Object, _contractRegistry.Object);
        var envelope = builder.Build(@event, _context);

        envelope.ContentType.Should().Be(EventEnvelope.DefaultContentType);
        envelope.ContentType.Should().Be("application/json");
    }

    private void SetupSerializerAndRegistry(
        EventClassification classification = EventClassification.Business)
    {
        _serializer
            .Setup(s => s.Serialize(It.IsAny<IIntegrationEvent>()))
            .Returns(new ReadOnlyMemory<byte>([1]));
        _contractRegistry
            .Setup(r => r.GetByType(typeof(FakeIntegrationEvent)))
            .Returns(new ContractDefinition
            {
                Name = "FakeIntegrationEvent",
                Version = 1,
                Classification = classification,
            });
    }
}

internal sealed record FakeIntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid? SourceEventId { get; init; }
    public string MessageName { get; init; } = "FakeEvent";
    public int SchemaVersion { get; init; } = 1;
    public Guid? AccountId { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public Guid? CausationId { get; init; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
