namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemLink : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid SourceItemId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public BoardItemLinkType LinkType { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private BoardItemLink() : base() { }

    public static BoardItemLink Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid sourceItemId,
        ResourceRef target,
        BoardItemLinkType type,
        Guid? createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(sourceItemId);
        Guard.NotNull(target);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(BusinessRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{target.WorkspaceId.Value}'.");

        Guard.NotEmpty(accountId);

        return new BoardItemLink
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            SourceItemId = sourceItemId,
            Target = target,
            LinkType = type,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };
    }
}
