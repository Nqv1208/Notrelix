using Notrelix.Domain.Common;
using Notrelix.Infrastructure.Events;

namespace Notrelix.Infrastructure.Tests.Events;

public class DomainEventDispatchPolicyTests
{
    private readonly IDomainEventDispatchPolicy _sut = new DomainEventDispatchPolicy();

    [Fact]
    public void GetMode_ForRegisteredEvent_ReturnsOutbox()
    {
        var mode = _sut.GetMode(typeof(WorkspaceCreatedDomainEvent));
        mode.Should().Be(DomainEventDispatchMode.Outbox);
    }

    [Fact]
    public void GetMode_ForUnregisteredEvent_ThrowsInvalidOperationException()
    {
        var unregistered = new FakeUnregisteredEvent();

        var act = () => _sut.GetMode(unregistered.GetType());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no dispatch policy*");
    }

    private sealed class FakeUnregisteredEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
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
        public int EventVersion { get; init; } = 1;
    }
}
