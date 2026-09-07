
namespace Notrelix.Application.Tests.Events.WorkManagement;

/// <summary>
/// TAC-WM-008 — Domain fact to Integration contract mappings preserve the
/// authoritative tenant envelope (AccountId, WorkspaceId), the acting
/// principal, and correlation/causation. TAC-WM-007 — the mapped contracts are
/// the pinned WorkManagement-owned events, never raw aggregates.
/// </summary>
public class WorkItemIntegrationEventMapperTests
{
    private readonly BoardEventMapper _boardMapper = new();
    private readonly BoardItemMemberAssignedEventMapper _memberMapper = new();

    [Fact]
    public void BoardItemMovedDomainEvent_Maps_AuthoritativeEnvelopeAndActor()
    {
        var domainEvent = new BoardItemMovedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            "a5", Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var mapped = _boardMapper.Map(domainEvent);

        mapped.Should().NotBeNull();
        mapped!.AccountId.Should().Be(domainEvent.AccountId,
            "the tenant envelope is authoritative from the producer Domain fact");
        mapped.WorkspaceId.Should().Be(domainEvent.WorkspaceId,
            "the tenant envelope is authoritative from the producer Domain fact");
        mapped.ItemId.Should().Be(domainEvent.ItemId);
        mapped.OldGroupId.Should().Be(domainEvent.OldGroupId);
        mapped.NewGroupId.Should().Be(domainEvent.NewGroupId);
        mapped.ActorUserId.Should().Be(domainEvent.UpdatedBy);
        mapped.CorrelationId.Should().Be(domainEvent.EventId);
        mapped.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void BoardItemMemberAssignedDomainEvent_Maps_WorkOwnedContract_WithEnvelope()
    {
        var domainEvent = new BoardItemMemberAssignedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var mapped = _memberMapper.Map(domainEvent);

        mapped.Should().NotBeNull();
        mapped!.AccountIdValue.Should().Be(domainEvent.AccountId);
        mapped.WorkspaceIdValue.Should().Be(domainEvent.WorkspaceId);
        mapped.ItemId.Should().Be(domainEvent.ItemId);
        mapped.AssignedUserId.Should().Be(domainEvent.UserId);
        mapped.AssignedBy.Should().Be(domainEvent.AssignedBy);
        mapped.CorrelationId.Should().Be(domainEvent.EventId);
        mapped.SourceEventId.Should().Be(domainEvent.EventId,
            "the source Domain fact is the causation link carried as SourceEventId");
        mapped.OccurredAt.Should().Be(domainEvent.OccurredAt);
    }

    [Fact]
    public void MappedContracts_ArePinnedWorkManagementOwned_Events()
    {
        var moved = _boardMapper.Map(new BoardItemMovedDomainEvent(
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), Guid.CreateVersion7(),
            "a5", Guid.CreateVersion7(), DateTimeOffset.UtcNow))!;

        moved.Should().BeAssignableTo<Notrelix.Application.Common.Events.IntegrationEvent>(
            "the mapped contract is the pinned WorkManagement-owned integration event, not a raw aggregate");
        moved.GetType().Namespace.Should().StartWith("Notrelix.Application.Events.WorkManagement",
            "TAC-WM-007: the contract is produced from WorkManagement ownership");
    }
}