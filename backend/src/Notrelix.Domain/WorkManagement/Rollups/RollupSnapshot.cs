using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Rollups;

public class RollupSnapshot : Entity
{
    public Guid ItemId { get; private set; }
    public Guid FieldId { get; private set; }
    public JsonValue Value { get; private set; } = null!;
    public DateTimeOffset UpdatedAt { get; private set; }

    private RollupSnapshot() : base() { }

    public static RollupSnapshot Create(Guid itemId, Guid fieldId, JsonValue value, DateTimeOffset updatedAt)
    {
        return new RollupSnapshot
        {
            ItemId = itemId,
            FieldId = fieldId,
            Value = value,
            UpdatedAt = updatedAt
        };
    }
}
