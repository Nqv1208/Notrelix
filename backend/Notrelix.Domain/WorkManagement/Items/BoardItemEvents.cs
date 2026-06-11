using Notrelix.Domain.Common;
using Notrelix.Domain.WorkManagement.Fields;

namespace Notrelix.Domain.WorkManagement.Items;

public record BoardItemCreatedEvent(Guid BoardId, Guid GroupId, Guid ItemId, string Name, Guid CreatedBy) : DomainRecordEvent;
public record BoardItemRenamedEvent(Guid ItemId, Guid BoardId, string OldName, string NewName, Guid UpdatedBy) : DomainRecordEvent;
public record BoardItemFieldValueChangedEvent(Guid ItemId, Guid BoardId, Guid FieldId, FieldValue OldValue, FieldValue NewValue, Guid UpdatedBy) : DomainRecordEvent;
public record BoardItemMovedEvent(Guid ItemId, Guid BoardId, Guid OldGroupId, Guid NewGroupId, double NewPosition, Guid UpdatedBy) : DomainRecordEvent;
public record BoardItemSoftDeletedEvent(Guid ItemId, Guid BoardId, Guid DeletedBy) : DomainRecordEvent;
public record BoardItemRestoredEvent(Guid ItemId, Guid BoardId, Guid RestoredBy) : DomainRecordEvent;
public record BoardItemArchivedEvent(Guid ItemId, Guid BoardId, Guid ArchivedBy) : DomainRecordEvent;
public record BoardItemMemberAssignedEvent(Guid ItemId, Guid UserId, Guid AssignedBy) : DomainRecordEvent;
public record BoardItemMemberUnassignedEvent(Guid ItemId, Guid UserId, Guid UnassignedBy) : DomainRecordEvent;
public record BoardItemLabelAddedEvent(Guid ItemId, Guid LabelId) : DomainRecordEvent;
public record BoardItemLabelRemovedEvent(Guid ItemId, Guid LabelId) : DomainRecordEvent;
public record BoardItemLinkedEvent(Guid SourceItemId, ResourceRef Target, BoardItemLinkType LinkType) : DomainRecordEvent;
