using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class GroupRule : ValueObject
{
    public Guid FieldId { get; }

    private GroupRule() { }    private GroupRule(Guid fieldId)
    {
        FieldId = fieldId;
    }

    public static GroupRule Create(Guid fieldId)
    {
        Guard.NotEmpty(fieldId);
        return new GroupRule(fieldId);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FieldId;
    }
}
