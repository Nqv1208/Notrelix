namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemLabel : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid LabelId { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private BoardItemLabel() : base() { }

    public static BoardItemLabel Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid labelId,
        Guid? createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(labelId);
        Guard.NotEmpty(accountId);

        return new BoardItemLabel
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ItemId = itemId,
            LabelId = labelId,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };
    }

    /// <summary>The label was put on the Item by a person; moving an Item with its labels is not this fact.</summary>
    public void RecordAddition(string labelName, Guid addedBy, DateTimeOffset addedAt)
    {
        Guard.NotEmpty(addedBy);
        RaiseDomainEvent(new Events.BoardItemLabelAddedDomainEvent(AccountId, WorkspaceId, ItemId, LabelId, labelName, addedBy, addedAt));
    }

    /// <summary>The label leaves the Item; the caller removes the link in the same unit of work.</summary>
    public void RecordRemoval(string labelName, Guid removedBy, DateTimeOffset removedAt)
    {
        Guard.NotEmpty(removedBy);
        RaiseDomainEvent(new Events.BoardItemLabelRemovedDomainEvent(AccountId, WorkspaceId, ItemId, LabelId, labelName, removedBy, removedAt));
    }
}
