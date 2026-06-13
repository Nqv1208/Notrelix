using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class SortRule : ValueObject
{
    public Guid FieldId { get; }
    public SortDirection Direction { get; }

    private SortRule() { }    private SortRule(Guid fieldId, SortDirection direction)
    {
        FieldId = fieldId;
        Direction = direction;
    }

    public static SortRule Create(Guid fieldId, SortDirection direction)
    {
        Guard.NotEmpty(fieldId);
        return new SortRule(fieldId, direction);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FieldId;
        yield return Direction;
    }
}
