namespace Notrelix.Domain.WorkManagement.Forms.Events;

public record FormQuestionAddedDomainEvent : DomainEvent
{
    public Guid FormId { get; }
    public string QuestionKey { get; }

    public FormQuestionAddedDomainEvent(
        Guid workspaceId,
        Guid formId,
        string questionKey,
        Guid? actorUserId,
        DateTimeOffset occurredAt)
        : base(occurredAt, workspaceId, actorUserId)
    {
        FormId = formId;
        QuestionKey = questionKey;
    }
}
