using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Security;

public sealed class SecurityEventMetadata : ValueObject
{
    public JsonValue Data { get; }

    private SecurityEventMetadata(JsonValue data)
    {
        Data = data;
    }

    public static SecurityEventMetadata Create(JsonValue data)
    {
        Guard.NotNull(data);
        return new SecurityEventMetadata(data);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Data;
    }
}
