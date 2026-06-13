using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Views;

public sealed class FilterRule : ValueObject
{
    public Guid FieldId { get; }
    public FilterOperator Operator { get; }
    public string? Value { get; }

    private FilterRule() { }    private FilterRule(Guid fieldId, FilterOperator op, string? value)
    {
        FieldId = fieldId;
        Operator = op;
        Value = value;
    }

    public static FilterRule Create(Guid fieldId, FilterOperator op, string? value)
    {
        Guard.NotEmpty(fieldId);
        return new FilterRule(fieldId, op, value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FieldId;
        yield return Operator;
        yield return Value;
    }
}
