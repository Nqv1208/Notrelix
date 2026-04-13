using TodoApp.Domain.Common;
using TodoApp.Domain.Enums;

namespace TodoApp.Domain.Entities;

public class Reaction : BaseEntity
{
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid UserId { get; private set; }
    public string Emoji { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Reaction() : base() { }

    public static Reaction Create(ResourceType resourceType, Guid resourceId, Guid userId, string emoji)
    {
        return new Reaction
        {
            ResourceType = resourceType,
            ResourceId = resourceId,
            UserId = userId,
            Emoji = emoji,
            CreatedAt = DateTime.UtcNow
        };
    }
}
