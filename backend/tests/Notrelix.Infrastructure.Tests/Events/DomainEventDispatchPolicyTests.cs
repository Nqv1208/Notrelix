using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Events;

namespace Notrelix.Infrastructure.Tests.Events;

public class ClassificationPolicyTests
{
    [Fact]
    public void GetClassification_UnregisteredEvent_ReturnsDefaultBusiness()
    {
        var sut = ClassificationPolicy.CreateBuilder().Build();
        var classification = sut.GetClassification(GetType());
        classification.Value.Should().Be(EventClassification.Business);
    }

    [Fact]
    public void GetClassification_RegisteredEvent_ReturnsCorrectClassification()
    {
        var sut = ClassificationPolicy.CreateBuilder()
            .Register<FakeEvent>(EventClassification.Audit)
            .Build();

        var classification = sut.GetClassification(typeof(FakeEvent));
        classification.Value.Should().Be(EventClassification.Audit);
    }
}

public class DeliveryPolicyTests
{
    [Fact]
    public void GetDecision_UnregisteredEvent_ReturnsOutboxOnly()
    {
        var sut = DeliveryPolicy.CreateBuilder().Build();
        var decision = sut.GetDecision(GetType());
        decision.Outbox.Should().BeTrue();
        decision.Realtime.Should().BeFalse();
    }

    [Fact]
    public void GetDecision_RegisteredEvent_ReturnsCorrectDecision()
    {
        var sut = DeliveryPolicy.CreateBuilder()
            .Register<FakeEvent>(outbox: false, realtime: true)
            .Build();

        var decision = sut.GetDecision(typeof(FakeEvent));
        decision.Outbox.Should().BeFalse();
        decision.Realtime.Should().BeTrue();
    }
}

internal sealed class FakeEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int EventVersion { get; init; } = 1;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string SourceContext { get; init; } = "test";
    public string AggregateType { get; init; } = "Fake";
    public Guid AggregateId { get; init; } = Guid.NewGuid();
    public string SubjectType { get; init; } = "Fake";
    public Guid SubjectId { get; init; } = Guid.NewGuid();
    public Guid? WorkspaceId { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
}
