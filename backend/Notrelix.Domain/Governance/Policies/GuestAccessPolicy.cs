using Notrelix.Domain.Common;

namespace Notrelix.Domain.Governance.Policies;

public sealed class GuestAccessPolicy : ValueObject
{
    public bool AllowGuestInvites { get; }
    public IReadOnlyCollection<string> AllowedDomains { get; }

    private GuestAccessPolicy(bool allowGuestInvites, IEnumerable<string>? allowedDomains)
    {
        AllowGuestInvites = allowGuestInvites;
        AllowedDomains = allowedDomains?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    public static GuestAccessPolicy Create(bool allowGuestInvites, IEnumerable<string>? allowedDomains = null)
    {
        return new GuestAccessPolicy(allowGuestInvites, allowedDomains);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AllowGuestInvites;
        foreach (var domain in AllowedDomains.OrderBy(d => d))
        {
            yield return domain;
        }
    }
}
