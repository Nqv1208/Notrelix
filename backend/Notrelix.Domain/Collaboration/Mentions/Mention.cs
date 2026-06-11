using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Mentions;

public class Mention : Entity
{
    public ResourceRef Source { get; private set; } = null!;
    public MentionType Type { get; private set; }
    public Guid MentionedId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Mention() : base() { }

    public static Mention Create(ResourceRef source, MentionType type, Guid mentionedId)
    {
        Guard.NotNull(source);
        Guard.NotEmpty(mentionedId);

        return new Mention
        {
            Source = source,
            Type = type,
            MentionedId = mentionedId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
