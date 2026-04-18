using Notrelix.Domain.Common;

namespace Notrelix.Domain.Entities;

public class BoardMember : BaseEntity
{
    public Guid BoardId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = "member";
    public DateTime JoinedAt { get; private set; }

    public Board Board { get; private set; } = null!;

    private BoardMember() : base() { }

    public static BoardMember Create(Guid boardId, Guid userId, string role = "member")
    {
        return new BoardMember
        {
            BoardId = boardId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };
    }
}
