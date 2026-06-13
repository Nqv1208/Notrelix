using Notrelix.Domain.Common;

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

        return new FormSubmission
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
    }

    public void Reject(DateTimeOffset processedAt)
    {
        Status = FormSubmissionStatus.Rejected;
        ProcessedAt = processedAt;
    }

    public void MarkAsSpam(DateTimeOffset processedAt)
    {
        Status = FormSubmissionStatus.Spam;
        ProcessedAt = processedAt;
    }

    public void MarkProcessed(Guid createdItemId, DateTimeOffset processedAt)
    {
        CreatedItemId = createdItemId;
        ProcessedAt = processedAt;
    }
}
