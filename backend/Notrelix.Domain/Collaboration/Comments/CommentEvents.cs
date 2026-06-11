using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Comments;

public record CommentCreatedEvent(Guid CommentId, ResourceRef Target, Guid CreatedBy) : DomainRecordEvent;
public record CommentUpdatedEvent(Guid CommentId, Guid UpdatedBy) : DomainRecordEvent;
public record CommentDeletedEvent(Guid CommentId, Guid DeletedBy) : DomainRecordEvent;
public record CommentResolvedEvent(Guid CommentId, Guid ResolvedBy) : DomainRecordEvent;
