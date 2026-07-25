namespace Notrelix.Domain.Collaboration.Comments;

public sealed class ParentCommentContext : ValueObject
{
    public Guid ParentCommentId { get; }
    public ResourceRef ParentTarget { get; }

    private ParentCommentContext(Guid parentCommentId, ResourceRef parentTarget)
    {
        ParentCommentId = parentCommentId;
        ParentTarget = parentTarget;
    }

    public static ParentCommentContext Create(Guid parentCommentId, ResourceRef parentTarget)
    {
        Guard.NotEmpty(parentCommentId);
        Guard.NotNull(parentTarget);

        return new ParentCommentContext(parentCommentId, parentTarget);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ParentCommentId;
        yield return ParentTarget;
    }
}
