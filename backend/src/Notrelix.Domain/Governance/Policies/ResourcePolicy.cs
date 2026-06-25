namespace Notrelix.Domain.Governance.Policies;

public sealed class ResourcePolicy : ValueObject
{
    public bool AllowPublicSharing { get; }

    private ResourcePolicy() { }
    private ResourcePolicy(bool allowPublicSharing)
    {
        AllowPublicSharing = allowPublicSharing;
    }

    public static ResourcePolicy Create(bool allowPublicSharing = false)
    {
        return new ResourcePolicy(allowPublicSharing);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowPublicSharing;
    }
}
