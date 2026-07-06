namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemMember : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    private BoardItemMember() : base() { }

    public static BoardItemMember Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid userId,
        Guid? assignedByUserId,
        DateTimeOffset assignedAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(accountId);

        return new BoardItemMember
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ItemId = itemId,
            UserId = userId,
            AssignedByUserId = assignedByUserId,
            AssignedAt = assignedAt
        };
    }
}
