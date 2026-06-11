using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Mentions;

public class Mention : Entity
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
