using Notrelix.Application.EventMappers.WorkManagement;

namespace Notrelix.Application.Tests.Events.WorkManagement;

/// <summary>
/// The Item lifecycle facts that had no outward contract (unarchived, deleted, restored, moved to another Board,
/// assignee removed) reach the outbox as Work-owned events with the authoritative tenant envelope and actor, and a
/// field value change carries readable JSON, not a CLR type name.
/// </summary>
public class ItemLifecycleIntegrationEventMapperTests
{
    private static readonly Guid Account = Guid.CreateVersion7();
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Item = Guid.CreateVersion7();
    private static readonly Guid Board = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly DateTimeOffset At = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    private readonly BoardItemLifecycleEventMapper _mapper = new();

    [Fact]
    public void Deleted_Restored_Unarchived_KeepEnvelopeActorAndSourceFact()
    {
        var deleted = new BoardItemDeletedDomainEvent(Account, Workspace, Item, Board, Actor, At);
        var restored = new BoardItemRestoredDomainEvent(Account, Workspace, Item, Board, Actor, At);
        var unarchived = new BoardItemUnarchivedDomainEvent(Account, Workspace, Board, Item, Actor, At);

        var d = _mapper.Map(deleted)!;
        var r = _mapper.Map(restored)!;
        var u = _mapper.Map(unarchived)!;

        d.DeletedBy.Should().Be(Actor);
        r.RestoredBy.Should().Be(Actor);
        u.UnarchivedBy.Should().Be(Actor);
        foreach (var (envelopeAccount, envelopeWorkspace, item, board, source, occurred) in new[]
                 {
                     (d.AccountIdValue, d.WorkspaceIdValue, d.ItemId, d.BoardId, d.SourceEventId, d.OccurredAt),
                     (r.AccountIdValue, r.WorkspaceIdValue, r.ItemId, r.BoardId, r.SourceEventId, r.OccurredAt),
                     (u.AccountIdValue, u.WorkspaceIdValue, u.ItemId, u.BoardId, u.SourceEventId, u.OccurredAt),
                 })
        {
            envelopeAccount.Should().Be(Account);
            envelopeWorkspace.Should().Be(Workspace);
            item.Should().Be(Item);
            board.Should().Be(Board);
            source.Should().NotBeNull();
            occurred.Should().Be(At);
        }

        d.SourceEventId.Should().Be(deleted.EventId);
        d.CorrelationId.Should().Be(deleted.EventId);
        d.ActorUserId.Should().Be(Actor, "the integration envelope's actor is the one who deleted it");
    }

    [Fact]
    public void MovedToBoard_CarriesBothBoardsBothGroupsTheKeyAndTheVersion()
    {
        var from = Guid.CreateVersion7();
        var to = Guid.CreateVersion7();
        var domainEvent = new BoardItemMovedToBoardDomainEvent(
            Account, Workspace, Item, from, to, Guid.CreateVersion7(), Guid.CreateVersion7(), "MOB-7", Actor, At, 5);

        var mapped = _mapper.Map(domainEvent)!;

        mapped.FromBoardId.Should().Be(from);
        mapped.ToBoardId.Should().Be(to);
        mapped.FromGroupId.Should().Be(domainEvent.FromGroupId);
        mapped.ToGroupId.Should().Be(domainEvent.ToGroupId);
        mapped.ItemKey.Should().Be("MOB-7");
        mapped.Revision.Should().Be(5);
        mapped.MovedBy.Should().Be(Actor);
        mapped.WorkspaceIdValue.Should().Be(Workspace);
    }

    [Fact]
    public void MemberUnassigned_MapsThePersonAndWhoRemovedThem()
    {
        var person = Guid.CreateVersion7();

        var mapped = _mapper.Map(new BoardItemMemberUnassignedDomainEvent(Account, Workspace, Item, person, Actor, At))!;

        mapped.UnassignedUserId.Should().Be(person);
        mapped.UnassignedBy.Should().Be(Actor);
    }

    [Fact]
    public void TheCompositeEntryPoint_MapsEachLifecycleFact_AndIgnoresOthers()
    {
        IIntegrationEventMapper mapper = _mapper;

        mapper.Map(new BoardItemDeletedDomainEvent(Account, Workspace, Item, Board, Actor, At)).Should().ContainSingle();
        mapper.Map(new BoardItemRestoredDomainEvent(Account, Workspace, Item, Board, Actor, At)).Should().ContainSingle();
        mapper.Map(new BoardItemUnarchivedDomainEvent(Account, Workspace, Board, Item, Actor, At)).Should().ContainSingle();
        mapper.Map(new BoardItemRenamedDomainEvent(Account, Workspace, Item, Board, "a", "b", Actor, At)).Should().BeEmpty();
    }

    [Fact]
    public void FieldValueChanged_CarriesTheStoredJson_NotATypeName()
    {
        var option = Guid.CreateVersion7();
        var domainEvent = new BoardItemFieldValueChangedDomainEvent(
            Account, Workspace, Item, Board, Guid.CreateVersion7(),
            FieldValue.Empty(), FieldValue.Create(JsonValue.Create($"\"{option}\"")), Actor, At);

        var mapped = new BoardEventMapper().Map(domainEvent)!;

        mapped.OldValue.Should().Be("null");
        mapped.NewValue.Should().Be($"\"{option}\"");
    }

    [Fact]
    public void LabelAddedAndRemoved_CarryNameActorEnvelopeAndSourceFact()
    {
        var label = Guid.CreateVersion7();
        var mapper = new BoardItemLabelEventMapper();
        var addedFact = new BoardItemLabelAddedDomainEvent(Account, Workspace, Item, label, "Bug", Actor, At);
        var removedFact = new BoardItemLabelRemovedDomainEvent(Account, Workspace, Item, label, "Bug", Actor, At);

        var added = mapper.Map(addedFact)!;
        var removed = mapper.Map(removedFact)!;

        added.LabelName.Should().Be("Bug");
        added.AddedBy.Should().Be(Actor);
        added.ActorUserId.Should().Be(Actor);
        added.SourceEventId.Should().Be(addedFact.EventId);
        removed.RemovedBy.Should().Be(Actor);
        removed.LabelId.Should().Be(label);
        foreach (var (envelopeAccount, envelopeWorkspace, item) in new[]
                 {
                     (added.AccountIdValue, added.WorkspaceIdValue, added.ItemId),
                     (removed.AccountIdValue, removed.WorkspaceIdValue, removed.ItemId),
                 })
        {
            envelopeAccount.Should().Be(Account);
            envelopeWorkspace.Should().Be(Workspace);
            item.Should().Be(Item);
        }
    }
}
