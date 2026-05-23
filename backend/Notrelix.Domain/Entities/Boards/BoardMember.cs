using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Boards;

public class BoardMember : BaseEntity
{
    public Guid BoardId { get; private set; }
    public Guid UserId { get; private set; }
    public BoardRole Role { get; private set; } = BoardRole.Member;
    public DateTime JoinedAt { get; private set; }

    public Board Board { get; private set; } = null!;

    private BoardMember() : base() { }

    public static BoardMember Create(Guid boardId, Guid userId, BoardRole role = BoardRole.Member)
    {
        return new BoardMember
        {
            BoardId = boardId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }

    public void UpdateRole(BoardRole newRole) => Role = newRole;
}
