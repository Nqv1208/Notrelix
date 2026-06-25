namespace Notrelix.Domain.Collaboration.Mentions;

public class Mention : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Source { get; private set; } = null!;
    public MentionType Type { get; private set; }
    public Guid MentionedId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Mention() : base() { }

    public static Mention Create(Guid workspaceId, ResourceRef source, MentionType type, Guid mentionedId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(source);
        Guard.NotEmpty(mentionedId);

        if (source.WorkspaceId.HasValue && source.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, source.WorkspaceId.Value);

        return new Mention
        {
            WorkspaceId = workspaceId,
            Source = source,
            Type = type,
            MentionedId = mentionedId,
            CreatedAt = createdAt
        };
    }
}
