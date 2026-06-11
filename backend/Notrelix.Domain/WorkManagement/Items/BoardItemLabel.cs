using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemLabel : Entity
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid LabelId { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private BoardItemLabel() : base() { }

    public static BoardItemLabel Create(
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

        return new BoardItemLabel
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ItemId = itemId,
            LabelId = labelId,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };
    }
}
