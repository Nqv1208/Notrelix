using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Comments;

public sealed class CommentAnchor : ValueObject
{
    public string? Selector { get; }
    public int? Offset { get; }

    private CommentAnchor(string? selector, int? offset)
    {
        Selector = selector;
        Offset = offset;
    }

    public static CommentAnchor Create(string? selector = null, int? offset = null)
    {
        return new CommentAnchor(selector, offset);
    }

    public static CommentAnchor None() => new(null, null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Selector;
        yield return Offset;
    }
}
