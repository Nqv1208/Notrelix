using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Reactions;



public class Reaction : Entity
{
    public ResourceRef Target { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public Emoji Emoji { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Reaction() : base() { }

    public static Reaction Create(ResourceRef target, Guid userId, Emoji emoji)
    {
        Guard.NotNull(target);
        Guard.NotEmpty(userId);
        Guard.NotNull(emoji);

        return new Reaction
        {
            Target = target,
            UserId = userId,
            Emoji = emoji,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
