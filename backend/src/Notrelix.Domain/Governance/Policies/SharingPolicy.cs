namespace Notrelix.Domain.Governance.Policies;

public sealed class SharingPolicy : ValueObject
{
    public bool AllowPublicSharing { get; }
    public bool AllowExternalInvite { get; }

    private SharingPolicy() { }
    private SharingPolicy(bool allowPublicSharing, bool allowExternalInvite)
    {
        AllowPublicSharing = allowPublicSharing;
        AllowExternalInvite = allowExternalInvite;
    }

    public static SharingPolicy Create(bool allowPublicSharing = false, bool allowExternalInvite = false)
    {
        return new SharingPolicy(allowPublicSharing, allowExternalInvite);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowPublicSharing;
        yield return AllowExternalInvite;
    }
}
