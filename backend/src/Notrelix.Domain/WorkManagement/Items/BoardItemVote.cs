namespace Notrelix.Domain.WorkManagement.Items;

/// <summary>One person's vote on an Item for one Vote field. One row per Item, field and person; removing the row withdraws the vote.</summary>
public class BoardItemVote : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset VotedAt { get; private set; }

    private BoardItemVote() : base() { }

    public static BoardItemVote Create(Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, Guid fieldId, Guid userId, DateTimeOffset votedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(fieldId);
        Guard.NotEmpty(userId);
        return new BoardItemVote
        {
            AccountId = accountId, WorkspaceId = workspaceId, BoardId = boardId, ItemId = itemId,
            FieldId = fieldId, UserId = userId, VotedAt = votedAt,
        };
    }
}
