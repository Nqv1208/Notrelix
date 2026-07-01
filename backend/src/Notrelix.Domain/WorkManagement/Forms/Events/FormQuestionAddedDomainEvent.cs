namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormQuestionAddedDomainEvent : WorkspaceScopedDomainEvent
{
    public Guid FormId { get; }
    public string QuestionKey { get; }

    public FormQuestionAddedDomainEvent(
        Guid accountId,
        Guid workspaceId,
        Guid formId,
        string questionKey,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(workspaceId, occurredAt, actorUserId)
    {
        FormId = formId;
        QuestionKey = questionKey;
    }
}
