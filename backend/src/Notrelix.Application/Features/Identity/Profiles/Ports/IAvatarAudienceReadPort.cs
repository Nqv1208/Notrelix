namespace Notrelix.Application.Features.Identity.Profiles.Ports;

/// <summary>
/// Identity-owned cross-context read port: whether two people take part in at least one
/// Workspace together. Identity decides who may see a profile photo (the person, and people they
/// share an active Workspace with); the membership fact is Workspaces-owned and read only through
/// this port, never by an Identity handler touching Workspaces data.
/// </summary>
public interface IAvatarAudienceReadPort
{
    Task<bool> ShareActiveWorkspaceAsync(Guid viewerId, Guid ownerId, CancellationToken cancellationToken);
}
