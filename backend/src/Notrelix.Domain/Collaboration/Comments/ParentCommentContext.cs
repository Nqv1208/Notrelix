namespace Notrelix.Domain.Collaboration.Comments;

public sealed class ParentCommentContext : ValueObject
{
    public Guid AccountId { get; }
    public Guid WorkspaceId { get; }
    public Guid ParentCommentId { get; }
    public ResourceRef ParentTarget { get; }
    public bool IsDeleted { get; }

    private ParentCommentContext(
        Guid accountId,
        Guid workspaceId,
        Guid parentCommentId,
        ResourceRef parentTarget,
        bool isDeleted)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        ParentCommentId = parentCommentId;
        ParentTarget = parentTarget;
        IsDeleted = isDeleted;
    }

    public static ParentCommentContext Create(
        Guid accountId,
        Guid workspaceId,
        Guid parentCommentId,
        ResourceRef parentTarget,
        bool isDeleted = false)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(parentCommentId);
        Guard.NotNull(parentTarget);

        return new ParentCommentContext(accountId, workspaceId, parentCommentId, parentTarget, isDeleted);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AccountId;
        yield return WorkspaceId;
        yield return ParentCommentId;
        yield return ParentTarget;
        yield return IsDeleted;
    }
}