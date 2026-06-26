namespace Notrelix.Domain.WorkManagement.Boards;

public class BoardMember : Entity
{
    public Guid BoardId { get; private set; }
    public Guid UserId { get; private set; }
    public BoardRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    private BoardMember() : base() { }

    public static BoardMember Create(Guid boardId, Guid userId, BoardRole role, DateTimeOffset joinedAt)
    {
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(userId);

        return new BoardMember
        {
            BoardId = boardId,
            UserId = userId,
            Role = role,
            JoinedAt = joinedAt
        };
    }

    public void UpdateRole(BoardRole role)
    {
        Role = role;
    }
}
