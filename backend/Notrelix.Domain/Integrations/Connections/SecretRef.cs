using Notrelix.Domain.Common;

namespace Notrelix.Domain.Integrations.Connections;

public sealed class SecretRef : ValueObject
{
    public string Key { get; }

    private SecretRef(string key)
    {
        Key = key;
    }

    public static SecretRef Create(string key)
    {
        Guard.NotNullOrWhiteSpace(key);
        return new SecretRef(key.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
    }
}
