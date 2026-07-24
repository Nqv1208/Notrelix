namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-question-added")]
public sealed record FormQuestionAddedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }
    public string QuestionKey { get; }

    public FormQuestionAddedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid formId,
        string questionKey,
        DateTimeOffset occurredAt)
        : base(accountId, workspaceId, occurredAt)
    {
        FormId = formId;
        QuestionKey = questionKey;
    }
}
