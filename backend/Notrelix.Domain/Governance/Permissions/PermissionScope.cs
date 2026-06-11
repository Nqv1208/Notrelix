using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Permissions;

public sealed class PermissionScope : ValueObject
{
    public string Value { get; }

    private PermissionScope(string value)
    {
        Value = value;
    }

    public static PermissionScope Create(string value)
    {
        Guard.NotNullOrWhiteSpace(value);
        return new PermissionScope(value.Trim().ToLowerInvariant());
    }

    public static PermissionScope All() => new("*");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
