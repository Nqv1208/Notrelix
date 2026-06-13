using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Forms;

public record FormSubmittedDomainEvent : DomainEvent
{
    public Guid FormId { get; }
    public Guid SubmissionId { get; }
    public Guid? CreatedItemId { get; }

    public FormSubmittedDomainEvent(
        Guid workspaceId,
        Guid formId,
        Guid submissionId,
        Guid? createdItemId,
        Guid? actorUserId,
        DateTimeOffset occurredAt) 
        : base(occurredAt, workspaceId, actorUserId)
    {
        FormId = formId;
        SubmissionId = submissionId;
        CreatedItemId = createdItemId;
    }
}
