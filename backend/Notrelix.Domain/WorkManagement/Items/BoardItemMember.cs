using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Items;

public class BoardItemMember : Entity
{
    public Guid ItemId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; }

    private BoardItemMember() : base() { }

    public static BoardItemMember Create(Guid itemId, Guid userId)
    {
        return new BoardItemMember
        {
            ItemId = itemId,
            UserId = userId,
            AssignedAt = DateTimeOffset.UtcNow
        };
    }
}
