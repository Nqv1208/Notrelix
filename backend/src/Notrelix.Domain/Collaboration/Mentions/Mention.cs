namespace Notrelix.Domain.Collaboration.Mentions;

using Notrelix.Domain.Collaboration.Mentions.Events;

public class Mention : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Source { get; private set; } = null!;
    public MentionType Type { get; private set; }
    public Guid MentionedId { get; private set; }
    public Guid MentionedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Mention() : base() { }

    public static Mention Create(Guid accountId, Guid workspaceId, ResourceRef source, MentionType type, Guid mentionedId, Guid mentionedByUserId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(source);
        Guard.NotEmpty(mentionedId);
        Guard.NotEmpty(mentionedByUserId);

        if (source.WorkspaceId.HasValue && source.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{source.WorkspaceId.Value}'.");

        var mention = new Mention
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Source = source,
            Type = type,
            MentionedId = mentionedId,
            MentionedByUserId = mentionedByUserId,
            CreatedAt = createdAt
        };

        mention.RaiseDomainEvent(new MentionCreatedDomainEvent(
            accountId, workspaceId, mention.Id, source, mentionedId, mentionedByUserId, createdAt));

        return mention;
    }
}
