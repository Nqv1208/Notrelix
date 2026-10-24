namespace Notrelix.Application.Features.Identity.OAuth.Abstractions;

/// <summary>
/// Identity-owned probe: whether a provider identity is already linked to any account. A boolean only; the
/// owner of someone else's link is never returned. A signed-in user's own link is read through the ordinary
/// own-row access.
/// </summary>
public interface IOAuthIdentityProbe
{
    Task<bool> IsLinkedAsync(string provider, string subject, CancellationToken cancellationToken);
}
