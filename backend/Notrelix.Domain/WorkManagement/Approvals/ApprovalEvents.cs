using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Approvals;

public record ApprovalRequestCreatedEvent(Guid RequestId, Guid WorkspaceId, ResourceRef Target) : DomainRecordEvent;
public record ApprovalRequestApprovedEvent(Guid RequestId, Guid DecidedBy) : DomainRecordEvent;
public record ApprovalRequestRejectedEvent(Guid RequestId, Guid DecidedBy, string? Note) : DomainRecordEvent;
public record ApprovalRequestCancelledEvent(Guid RequestId, Guid CancelledBy) : DomainRecordEvent;
