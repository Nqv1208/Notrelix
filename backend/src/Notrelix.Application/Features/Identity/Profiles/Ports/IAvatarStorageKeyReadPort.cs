namespace Notrelix.Application.Features.Identity.Profiles.Ports;

/// <summary>
/// Identity-owned read port for the object-store key of another person's uploaded profile photo. The
/// adapter resolves the key only for people the caller shares a workspace membership with (in any
/// membership state) and answers <c>null</c> for everyone else, exactly as for a person without a photo.
/// Whether the caller may actually SEE the photo (an active shared Workspace) stays the handler's rule.
/// </summary>
public interface IAvatarStorageKeyReadPort
{
    Task<string?> FindAsync(Guid ownerId, CancellationToken cancellationToken);
}
