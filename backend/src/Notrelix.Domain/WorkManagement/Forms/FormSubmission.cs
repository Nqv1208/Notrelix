using Notrelix.Domain.WorkManagement.Forms.Events;

namespace Notrelix.Domain.WorkManagement.Forms;

public class FormSubmission : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
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
    public string? RejectionReason { get; private set; }

    private FormSubmission() : base() { }

    private static string ValidateJson(string? value)
    {
        var json = value ?? "{}";
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormQuestion_InvalidConfigJson, "Payload must be valid JSON.");
        }
        return json;
    }

    public static FormSubmission Create(
        Guid accountId,
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
        Guard.NotEmpty(accountId);

        var submission = new FormSubmission
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            FormId = formId,
            BoardId = boardId,
            CreatedItemId = createdItemId,
            SubmitterUserId = submitterUserId,
            SubmitterEmail = submitterEmail,
            PayloadJson = ValidateJson(payloadJson),
            SourceIp = sourceIp,
            UserAgent = userAgent,
            Status = FormSubmissionStatus.Accepted,
            SubmittedAt = submittedAt
        };

        submission.RaiseDomainEvent(new FormSubmissionCreatedDomainEvent(accountId, workspaceId, submission.Id, formId, boardId, submitterUserId, submittedAt));

        return submission;
    }

    public void Reject(DateTimeOffset processedAt, string? reason = null)
    {
        if (Status != FormSubmissionStatus.Accepted)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormSubmission_CannotRejectUnlessAccepted, "Only accepted submissions can be rejected.");

        Status = FormSubmissionStatus.Rejected;
        RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ProcessedAt = processedAt;
        RaiseDomainEvent(new FormSubmissionRejectedDomainEvent(AccountId, WorkspaceId, Id, FormId, processedAt));
    }

    public void MarkAsSpam(DateTimeOffset processedAt)
    {
        if (Status != FormSubmissionStatus.Accepted)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormSubmission_CannotMarkSpamUnlessAccepted, "Only accepted submissions can be marked as spam.");

        Status = FormSubmissionStatus.Spam;
        ProcessedAt = processedAt;
        RaiseDomainEvent(new FormSubmissionMarkedAsSpamDomainEvent(AccountId, WorkspaceId, Id, FormId, processedAt));
    }

    public void MarkProcessed(Guid createdItemId, DateTimeOffset processedAt)
    {
        if (Status != FormSubmissionStatus.Accepted)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormSubmission_CannotProcessUnlessAccepted, "Only accepted submissions can be processed.");

        // Processing twice with the same item is a no-op; another item would hide the one already made.
        if (CreatedItemId == createdItemId) return;
        if (CreatedItemId is not null)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormSubmission_AlreadyProcessed, "This response was already processed into an item.");

        CreatedItemId = createdItemId;
        ProcessedAt = processedAt;
        RaiseDomainEvent(new FormSubmissionProcessedDomainEvent(AccountId, WorkspaceId, Id, FormId, createdItemId, processedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt)
    {
        if (Status == FormSubmissionStatus.Deleted)
            throw new BusinessRuleException(WorkManagementRuleCodes.WorkManagement_FormSubmission_AlreadyDeleted, "Submission is already deleted.");

        Status = FormSubmissionStatus.Deleted;
        RaiseDomainEvent(new FormSubmissionDeletedDomainEvent(AccountId, WorkspaceId, Id, FormId, deletedBy, deletedAt));
    }
}
