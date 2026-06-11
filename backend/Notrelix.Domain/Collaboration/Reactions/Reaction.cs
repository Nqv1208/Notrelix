using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Reactions;

public sealed class Emoji : ValueObject
{
    public string Code { get; }

    private Emoji(string code)
    {
        Code = code;
    }

    public static Emoji Create(string code)
    {
        Guard.NotNullOrWhiteSpace(code);
        return new Emoji(code.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }
}

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
