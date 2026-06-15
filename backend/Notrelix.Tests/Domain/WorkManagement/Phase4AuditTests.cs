using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Approvals.Events;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Boards.Events;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Checklists.Events;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Fields.Events;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Forms.Events;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Items.Events;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.WorkManagement.Labels.Events;
using Notrelix.Domain.WorkManagement.Relations;
using Notrelix.Domain.WorkManagement.Relations.Events;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.WorkManagement.Views.Events;
using Xunit;

namespace Notrelix.Domain.Tests.WorkManagement;

public class Phase4AuditTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid BoardA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    #region Task 9 — Board events (UpdateDescription, UpdateBackground, SetDefaultGroup)

    [Fact]
    public void Board_UpdateDescription_ShouldRaiseEvent()
    {
        var board = Board.Create(WsA, Actor, "Board", null, Now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.UpdateDescription("New desc", Actor, Now);

        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardDescriptionUpdatedEvent);
        var evt = (BoardDescriptionUpdatedEvent)board.DomainEvents.Single(e => e is BoardDescriptionUpdatedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.BoardId.Should().Be(board.Id);
        evt.OldDescription.Should().BeNull();
        evt.NewDescription.Should().Be("New desc");
    }

    [Fact]
    public void Board_UpdateDescription_WhenSameValue_ShouldNotRaiseEvent()
    {
        var board = Board.Create(WsA, Actor, "Board", "desc", Now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.UpdateDescription("desc", Actor, Now);

        board.Version.Should().Be(version);
        board.DomainEvents.Should().NotContain(e => e is BoardDescriptionUpdatedEvent);
    }

    [Fact]
    public void Board_UpdateBackground_ShouldRaiseEvent()
    {
        var board = Board.Create(WsA, Actor, "Board", null, Now);
        board.ClearDomainEvents();
        var version = board.Version;

        board.UpdateBackground("new-bg", Actor, Now);

        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardBackgroundUpdatedEvent);
        var evt = (BoardBackgroundUpdatedEvent)board.DomainEvents.Single(e => e is BoardBackgroundUpdatedEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.BoardId.Should().Be(board.Id);
        evt.NewBackground.Should().Be("new-bg");
    }

    [Fact]
    public void Board_SetDefaultGroup_ShouldRaiseEvent()
    {
        var board = Board.Create(WsA, Actor, "Board", null, Now);
        board.ClearDomainEvents();
        var version = board.Version;
        var groupId = Guid.NewGuid();

        board.SetDefaultGroup(groupId, Actor, Now);

        board.Version.Should().Be(version + 1);
        board.DomainEvents.Should().ContainSingle(e => e is BoardDefaultGroupSetEvent);
        var evt = (BoardDefaultGroupSetEvent)board.DomainEvents.Single(e => e is BoardDefaultGroupSetEvent);
        evt.GroupId.Should().Be(groupId);
    }

    #endregion

    #region Task 10 — BoardField events (UpdateClassification, UpdateFormula, Restore)

    [Fact]
    public void BoardField_UpdateClassification_ShouldRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now);
        field.ClearDomainEvents();
        var version = field.Version;

        field.UpdateClassification(DataClassification.Confidential, true, Actor, Now);

        field.Version.Should().Be(version + 1);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldClassificationUpdatedEvent);
        var evt = (BoardFieldClassificationUpdatedEvent)field.DomainEvents.Single(e => e is BoardFieldClassificationUpdatedEvent);
        evt.Classification.Should().Be(DataClassification.Confidential);
        evt.IsSensitive.Should().BeTrue();
    }

    [Fact]
    public void BoardField_UpdateClassification_WhenSameValue_ShouldNotRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now,
            dataClassification: DataClassification.Confidential, isSensitive: true);
        field.ClearDomainEvents();
        var version = field.Version;

        field.UpdateClassification(DataClassification.Confidential, true, Actor, Now);

        field.Version.Should().Be(version);
        field.DomainEvents.Should().NotContain(e => e is BoardFieldClassificationUpdatedEvent);
    }

    [Fact]
    public void BoardField_UpdateFormula_ShouldRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now);
        field.ClearDomainEvents();
        var version = field.Version;

        field.UpdateFormula(true, "CONCAT(a, b)", Actor, Now);

        field.Version.Should().Be(version + 1);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldFormulaUpdatedEvent);
        var evt = (BoardFieldFormulaUpdatedEvent)field.DomainEvents.Single(e => e is BoardFieldFormulaUpdatedEvent);
        evt.IsFormula.Should().BeTrue();
        evt.Expression.Should().Be("CONCAT(a, b)");
    }

    [Fact]
    public void BoardField_Restore_ShouldRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now);
        field.SoftDelete(Actor, Now);
        field.ClearDomainEvents();
        var version = field.Version;

        field.Restore(Actor, Now);

        field.IsDeleted.Should().BeFalse();
        field.Version.Should().Be(version + 1);
        field.DomainEvents.Should().ContainSingle(e => e is BoardFieldRestoredEvent);
    }

    [Fact]
    public void BoardField_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var position = FractionalIndex.Create("a0");
        var field = BoardField.Create(WsA, BoardA, "Field", FieldType.Text, FieldSettings.Empty(), position, Actor, Now);
        field.ClearDomainEvents();
        var version = field.Version;

        field.Restore(Actor, Now);

        field.Version.Should().Be(version);
        field.DomainEvents.Should().NotContain(e => e is BoardFieldRestoredEvent);
    }

    #endregion

    #region Task 11 — BoardItem events (Complete, SetTimeline, AssignParentItem)

    [Fact]
    public void BoardItem_Complete_ShouldRaiseEvent()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;

        item.Complete(Now, Actor, Now);

        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemCompletedEvent);
        var evt = (BoardItemCompletedEvent)item.DomainEvents.Single(e => e is BoardItemCompletedEvent);
        evt.CompletedAt.Should().Be(Now);
        evt.CompletedBy.Should().Be(Actor);
    }

    [Fact]
    public void BoardItem_Complete_WhenSameValue_ShouldNotRaiseEvent()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.Complete(Now, Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;

        item.Complete(Now, Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemCompletedEvent);
    }

    [Fact]
    public void BoardItem_SetTimeline_ShouldRaiseEvent()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;

        item.SetTimeline(Now, Now.AddDays(7), Actor, Now);

        item.Version.Should().Be(version + 1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemTimelineSetEvent);
        var evt = (BoardItemTimelineSetEvent)item.DomainEvents.Single(e => e is BoardItemTimelineSetEvent);
        evt.StartedAt.Should().Be(Now);
        evt.DueAt.Should().Be(Now.AddDays(7));
    }

    [Fact]
    public void BoardItem_SetTimeline_WhenSameValue_ShouldNotRaiseEvent()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now, startedAt: Now, dueAt: Now.AddDays(7));
        item.ClearDomainEvents();
        var version = item.Version;

        item.SetTimeline(Now, Now.AddDays(7), Actor, Now);

        item.Version.Should().Be(version);
        item.DomainEvents.Should().NotContain(e => e is BoardItemTimelineSetEvent);
    }

    [Fact]
    public void BoardItem_AssignParentItem_ShouldRaiseEvent()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);
        item.ClearDomainEvents();
        var version = item.Version;
        var parentId = Guid.NewGuid();

        item.AssignParentItem(parentId, 1, Actor, Now);

        item.Version.Should().Be(version + 1);
        item.ParentItemId.Should().Be(parentId);
        item.ItemLevel.Should().Be(1);
        item.DomainEvents.Should().ContainSingle(e => e is BoardItemParentAssignedEvent);
        var evt = (BoardItemParentAssignedEvent)item.DomainEvents.Single(e => e is BoardItemParentAssignedEvent);
        evt.ParentItemId.Should().Be(parentId);
        evt.ItemLevel.Should().Be(1);
    }

    [Fact]
    public void BoardItem_AssignParentItem_WithOwnId_ShouldThrow()
    {
        var item = BoardItem.Create(WsA, BoardA, Guid.NewGuid(), "Item", FractionalIndex.Create("a0"), Actor, Now);

        var act = () => item.AssignParentItem(item.Id, 0, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*own parent*");
    }

    #endregion

    #region Task 12 — BoardRelation events (Pause, Resume, MarkBroken)

    [Fact]
    public void BoardRelation_Pause_ShouldRaiseEvent()
    {
        var relation = BoardRelation.Create(WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.ClearDomainEvents();
        var version = relation.Version;

        relation.Pause(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Paused);
        relation.Version.Should().Be(version + 1);
        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationPausedEvent);
    }

    [Fact]
    public void BoardRelation_Pause_WhenAlreadyPaused_ShouldNotRaiseEvent()
    {
        var relation = BoardRelation.Create(WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.Pause(Actor, Now);
        relation.ClearDomainEvents();
        var version = relation.Version;

        relation.Pause(Actor, Now);

        relation.Version.Should().Be(version);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationPausedEvent);
    }

    [Fact]
    public void BoardRelation_Resume_ShouldRaiseEvent()
    {
        var relation = BoardRelation.Create(WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.Pause(Actor, Now);
        relation.ClearDomainEvents();
        var version = relation.Version;

        relation.Resume(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Active);
        relation.Version.Should().Be(version + 1);
        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationResumedEvent);
    }

    [Fact]
    public void BoardRelation_Resume_WhenAlreadyActive_ShouldNotRaiseEvent()
    {
        var relation = BoardRelation.Create(WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.ClearDomainEvents();
        var version = relation.Version;

        relation.Resume(Actor, Now);

        relation.Version.Should().Be(version);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationResumedEvent);
    }

    [Fact]
    public void BoardRelation_MarkBroken_ShouldRaiseEvent()
    {
        var relation = BoardRelation.Create(WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.ClearDomainEvents();
        var version = relation.Version;

        relation.MarkBroken(Actor, Now);

        relation.Status.Should().Be(BoardRelationStatus.Broken);
        relation.Version.Should().Be(version + 1);
        relation.DomainEvents.Should().ContainSingle(e => e is BoardRelationMarkedBrokenEvent);
    }

    [Fact]
    public void BoardRelation_MarkBroken_WhenAlreadyBroken_ShouldNotRaiseEvent()
    {
        var relation = BoardRelation.Create(WsA, BoardA, Guid.NewGuid(), null, null, Actor, Now);
        relation.MarkBroken(Actor, Now);
        relation.ClearDomainEvents();
        var version = relation.Version;

        relation.MarkBroken(Actor, Now);

        relation.Version.Should().Be(version);
        relation.DomainEvents.Should().NotContain(e => e is BoardRelationMarkedBrokenEvent);
    }

    #endregion

    #region Task 13 — BoardView Restore

    [Fact]
    public void BoardView_Restore_ShouldRaiseEvent()
    {
        var config = BoardViewConfig.Create(JsonValue.EmptyObject());
        var view = BoardView.Create(WsA, BoardA, "View", ViewType.Table, config, Actor, Now);
        view.SoftDelete(Actor, Now);
        view.ClearDomainEvents();
        var version = view.Version;

        view.Restore(Actor, Now);

        view.IsDeleted.Should().BeFalse();
        view.Version.Should().Be(version + 1);
        view.DomainEvents.Should().ContainSingle(e => e is BoardViewRestoredEvent);
    }

    [Fact]
    public void BoardView_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var config = BoardViewConfig.Create(JsonValue.EmptyObject());
        var view = BoardView.Create(WsA, BoardA, "View", ViewType.Table, config, Actor, Now);
        view.ClearDomainEvents();
        var version = view.Version;

        view.Restore(Actor, Now);

        view.Version.Should().Be(version);
        view.DomainEvents.Should().NotContain(e => e is BoardViewRestoredEvent);
    }

    #endregion

    #region Task 14 — SavedFilter events

    [Fact]
    public void SavedFilter_Rename_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.Rename("Renamed", Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterRenamedEvent);
        var evt = (SavedFilterRenamedEvent)filter.DomainEvents.Single(e => e is SavedFilterRenamedEvent);
        evt.Name.Should().Be("Renamed");
    }

    [Fact]
    public void SavedFilter_UpdateVisibility_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateVisibility(SavedFilterVisibility.Public, Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterVisibilityUpdatedEvent);
    }

    [Fact]
    public void SavedFilter_UpdateFilters_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateFilters(new[] { FilterRule.Create(Guid.NewGuid(), FilterOperator.NotEquals, "other") }, Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterFiltersUpdatedEvent);
    }

    [Fact]
    public void SavedFilter_UpdateSorts_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateSorts(new[] { SortRule.Create(Guid.NewGuid(), SortDirection.Ascending) }, Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterSortsUpdatedEvent);
    }

    [Fact]
    public void SavedFilter_UpdateGroup_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.UpdateGroup(GroupRule.Create(Guid.NewGuid()), Actor, Now);

        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterGroupUpdatedEvent);
    }

    [Fact]
    public void SavedFilter_SoftDelete_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.SoftDelete(Actor, Now);

        filter.IsDeleted.Should().BeTrue();
        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterSoftDeletedEvent);
    }

    [Fact]
    public void SavedFilter_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.SoftDelete(Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.SoftDelete(Actor, Now);

        filter.Version.Should().Be(version);
        filter.DomainEvents.Should().NotContain(e => e is SavedFilterSoftDeletedEvent);
    }

    [Fact]
    public void SavedFilter_Restore_ShouldRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.SoftDelete(Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.Restore(Actor, Now);

        filter.IsDeleted.Should().BeFalse();
        filter.Version.Should().Be(version + 1);
        filter.DomainEvents.Should().ContainSingle(e => e is SavedFilterRestoredEvent);
    }

    [Fact]
    public void SavedFilter_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var filterRule = FilterRule.Create(Guid.NewGuid(), FilterOperator.Equals, "val");
        var filter = SavedFilter.Create(WsA, BoardA, "My Filter", new[] { filterRule }, Actor, Now);
        filter.ClearDomainEvents();
        var version = filter.Version;

        filter.Restore(Actor, Now);

        filter.Version.Should().Be(version);
        filter.DomainEvents.Should().NotContain(e => e is SavedFilterRestoredEvent);
    }

    #endregion

    #region Task 15 — Label.Restore + Checklist.SoftDelete/Restore + ApprovalRequest.SoftDelete/Restore

    [Fact]
    public void Label_Restore_ShouldRaiseEvent()
    {
        var label = Label.Create(WsA, BoardA, "Bug", LabelColor.Create("#FF0000"), Actor, Now);
        label.SoftDelete(Actor, Now);
        label.ClearDomainEvents();
        var version = label.Version;

        label.Restore(Actor, Now);

        label.IsDeleted.Should().BeFalse();
        label.Version.Should().Be(version + 1);
        label.DomainEvents.Should().ContainSingle(e => e is LabelRestoredEvent);
    }

    [Fact]
    public void Label_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var label = Label.Create(WsA, BoardA, "Bug", LabelColor.Create("#FF0000"), Actor, Now);
        label.ClearDomainEvents();
        var version = label.Version;

        label.Restore(Actor, Now);

        label.Version.Should().Be(version);
        label.DomainEvents.Should().NotContain(e => e is LabelRestoredEvent);
    }

    [Fact]
    public void Checklist_SoftDelete_ShouldRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.SoftDelete(Actor, Now);

        checklist.IsDeleted.Should().BeTrue();
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistSoftDeletedEvent);
    }

    [Fact]
    public void Checklist_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.SoftDelete(Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.SoftDelete(Actor, Now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().NotContain(e => e is ChecklistSoftDeletedEvent);
    }

    [Fact]
    public void Checklist_Restore_ShouldRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.SoftDelete(Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.Restore(Actor, Now);

        checklist.IsDeleted.Should().BeFalse();
        checklist.Version.Should().Be(version + 1);
        checklist.DomainEvents.Should().ContainSingle(e => e is ChecklistRestoredEvent);
    }

    [Fact]
    public void Checklist_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var checklist = Checklist.Create(WsA, Guid.NewGuid(), "Checklist", FractionalIndex.Create("a0"), Actor, Now);
        checklist.ClearDomainEvents();
        var version = checklist.Version;

        checklist.Restore(Actor, Now);

        checklist.Version.Should().Be(version);
        checklist.DomainEvents.Should().NotContain(e => e is ChecklistRestoredEvent);
    }

    [Fact]
    public void ApprovalRequest_SoftDelete_ShouldRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(WsA, target, "Approve this", Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.SoftDelete(Actor, Now);

        request.IsDeleted.Should().BeTrue();
        request.Version.Should().Be(version + 1);
        request.DomainEvents.Should().ContainSingle(e => e is ApprovalRequestSoftDeletedEvent);
    }

    [Fact]
    public void ApprovalRequest_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(WsA, target, "Approve this", Actor, Now);
        request.SoftDelete(Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.SoftDelete(Actor, Now);

        request.Version.Should().Be(version);
        request.DomainEvents.Should().NotContain(e => e is ApprovalRequestSoftDeletedEvent);
    }

    [Fact]
    public void ApprovalRequest_Restore_ShouldRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(WsA, target, "Approve this", Actor, Now);
        request.SoftDelete(Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.Restore(Actor, Now);

        request.IsDeleted.Should().BeFalse();
        request.Version.Should().Be(version + 1);
        request.DomainEvents.Should().ContainSingle(e => e is ApprovalRequestRestoredEvent);
    }

    [Fact]
    public void ApprovalRequest_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var target = ResourceRef.Create(ResourceType.BoardItem, Guid.NewGuid(), WsA);
        var request = ApprovalRequest.Create(WsA, target, "Approve this", Actor, Now);
        request.ClearDomainEvents();
        var version = request.Version;

        request.Restore(Actor, Now);

        request.Version.Should().Be(version);
        request.DomainEvents.Should().NotContain(e => e is ApprovalRequestRestoredEvent);
    }

    #endregion

    #region Task 16 — Form events (UpdateDetails, SoftDelete, Restore)

    [Fact]
    public void Form_UpdateDetails_ShouldRaiseEvent()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.UpdateDetails("Updated Form", BoardVisibility.Workspace, "{}", "{}", Actor, Now);

        form.Version.Should().Be(version + 1);
        form.DomainEvents.Should().ContainSingle(e => e is FormDetailsUpdatedEvent);
        var evt = (FormDetailsUpdatedEvent)form.DomainEvents.Single(e => e is FormDetailsUpdatedEvent);
        evt.Name.Should().Be("Updated Form");
    }

    [Fact]
    public void Form_SoftDelete_ShouldRaiseEvent()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.SoftDelete(Actor, Now);

        form.IsDeleted.Should().BeTrue();
        form.Version.Should().Be(version + 1);
        form.DomainEvents.Should().ContainSingle(e => e is FormSoftDeletedEvent);
    }

    [Fact]
    public void Form_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.SoftDelete(Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.SoftDelete(Actor, Now);

        form.Version.Should().Be(version);
        form.DomainEvents.Should().NotContain(e => e is FormSoftDeletedEvent);
    }

    [Fact]
    public void Form_Restore_ShouldRaiseEvent()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.SoftDelete(Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.Restore(Actor, Now);

        form.IsDeleted.Should().BeFalse();
        form.Version.Should().Be(version + 1);
        form.DomainEvents.Should().ContainSingle(e => e is FormRestoredEvent);
    }

    [Fact]
    public void Form_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var form = Form.Create(WsA, BoardA, "Form", "form", Actor, Now);
        form.ClearDomainEvents();
        var version = form.Version;

        form.Restore(Actor, Now);

        form.Version.Should().Be(version);
        form.DomainEvents.Should().NotContain(e => e is FormRestoredEvent);
    }

    #endregion
}
