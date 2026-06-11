using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.BoardGroups;

public record BoardGroupCreatedEvent(Guid BoardId, Guid GroupId, string Title, Guid CreatedBy) : DomainRecordEvent;
public record BoardGroupRenamedEvent(Guid GroupId, Guid BoardId, string OldTitle, string NewTitle, Guid UpdatedBy) : DomainRecordEvent;
public record BoardGroupReorderedEvent(Guid GroupId, Guid BoardId, double NewPosition, Guid UpdatedBy) : DomainRecordEvent;
public record BoardGroupArchivedEvent(Guid GroupId, Guid ArchivedBy) : DomainRecordEvent;
public record BoardGroupSoftDeletedEvent(Guid GroupId, Guid DeletedBy) : DomainRecordEvent;
