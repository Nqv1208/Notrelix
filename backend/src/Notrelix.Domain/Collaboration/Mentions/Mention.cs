namespace Notrelix.Domain.Collaboration.Mentions;

public class Mention : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Source { get; private set; } = null!;
    public MentionType Type { get; private set; }
    public Guid MentionedId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Mention() : base() { }

    public static Mention Create(Guid accountId, Guid workspaceId, ResourceRef source, MentionType type, Guid mentionedId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(source);
        Guard.NotEmpty(mentionedId);

        if (source.WorkspaceId.HasValue && source.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(BusinessRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{source.WorkspaceId.Value}'.");

        return new Mention
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Source = source,
            Type = type,
            MentionedId = mentionedId,
            CreatedAt = createdAt
        };
    }
}
