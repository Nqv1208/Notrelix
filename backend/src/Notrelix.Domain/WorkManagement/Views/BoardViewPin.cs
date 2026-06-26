namespace Notrelix.Domain.WorkManagement.Views;

public enum BoardViewPinScope
{
    User,
    BoardDefault
}

public class BoardViewPin : Entity, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid BoardViewId { get; private set; }
    public Guid? UserId { get; private set; }
    public BoardViewPinScope PinScope { get; private set; }
    public FractionalIndex Position { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    private BoardViewPin() : base() { }

    public static BoardViewPin Create(
        Guid workspaceId,
        Guid boardId,
        Guid boardViewId,
        Guid? userId,
        BoardViewPinScope pinScope,
        FractionalIndex position,
        Guid? createdBy,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(boardViewId);
        Guard.NotNull(position);

        return new BoardViewPin
        {
            WorkspaceId = workspaceId,
            BoardId = boardId,
            BoardViewId = boardViewId,
            UserId = userId,
            PinScope = pinScope,
            Position = position,
            CreatedBy = createdBy,
            CreatedAt = createdAt
        };
    }
}
