namespace Notrelix.Application.Features.Identity.Public.DefaultWorkspace;

/// <summary>
/// Producer-owned public surface for the person's default Workspace: the one that opens with the
/// app. Identity stores only the chosen Workspace id on the person's profile. Whether the person
/// may open that Workspace is a Workspaces fact: Workspaces validates membership before
/// <see cref="SetAsync"/> and calls <see cref="ClearIfAsync"/> when the person leaves, and a reader
/// treats a default the person cannot open as absent. Every member acts on the principal's own
/// profile; the subject is never taken from request input.
/// </summary>
public interface IIdentityDefaultWorkspace
{
    /// <summary>The chosen default Workspace id, or null when none is chosen.</summary>
    Task<Guid?> GetAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Chooses the default Workspace. Choosing the current value changes nothing.</summary>
    Task SetAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);

    /// <summary>Clears the default only when it is <paramref name="workspaceId"/>; another default is kept.</summary>
    Task ClearIfAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken);
}
