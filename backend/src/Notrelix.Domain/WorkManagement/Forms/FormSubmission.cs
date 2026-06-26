using Notrelix.Domain.WorkManagement.Forms.Events;

namespace Notrelix.Domain.WorkManagement.Forms;

public class FormSubmission : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid FormId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid? CreatedItemId { get; private set; }
    public Guid? SubmitterUserId { get; private set; }
    public string? SubmitterEmail { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public string? SourceIp { get; private set; }
    public string? UserAgent { get; private set; }
    public FormSubmissionStatus Status { get; private set; } = FormSubmissionStatus.Accepted;
    public DateTimeOffset SubmittedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }

    private FormSubmission() : base() { }

    public static FormSubmission Create(
        Guid workspaceId,
        Guid formId,
        Guid boardId,
        Guid? createdItemId,
        Guid? submitterUserId,
        string? submitterEmail,
        string payloadJson,
        string? sourceIp,
        string? userAgent,
        DateTimeOffset submittedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(formId);
        Guard.NotEmpty(boardId);

        var submission = new FormSubmission
        {
            WorkspaceId = workspaceId,
            FormId = formId,
            BoardId = boardId,
            CreatedItemId = createdItemId,
            SubmitterUserId = submitterUserId,
            SubmitterEmail = submitterEmail,
            PayloadJson = payloadJson ?? "{}",
            SourceIp = sourceIp,
            UserAgent = userAgent,
            Status = FormSubmissionStatus.Accepted,
            SubmittedAt = submittedAt
        };

        submission.AddDomainEvent(new FormSubmissionCreatedDomainEvent(workspaceId, submission.Id, formId, boardId, submitterUserId, submittedAt));

        return submission;
    }

    public void Reject(DateTimeOffset processedAt)
    {
        Status = FormSubmissionStatus.Rejected;
        ProcessedAt = processedAt;
        AddDomainEvent(new FormSubmissionRejectedDomainEvent(WorkspaceId, Id, FormId, processedAt));
    }

    public void MarkAsSpam(DateTimeOffset processedAt)
    {
        Status = FormSubmissionStatus.Spam;
        ProcessedAt = processedAt;
        AddDomainEvent(new FormSubmissionMarkedAsSpamDomainEvent(WorkspaceId, Id, FormId, processedAt));
    }

    public void MarkProcessed(Guid createdItemId, DateTimeOffset processedAt)
    {
        CreatedItemId = createdItemId;
        ProcessedAt = processedAt;
        AddDomainEvent(new FormSubmissionProcessedDomainEvent(WorkspaceId, Id, FormId, createdItemId, processedAt));
    }
}
