namespace Notrelix.Application.Common.Tenancy;

/// <summary>
/// The display name a change notice ("changed by", "deleted by") may carry. Only the name, never the email.
/// A Workspace Guest reads the neutral copy instead of a name, and so does anyone the viewer has no
/// workspace relationship to (the co-member profile lookup returns nobody then).
/// </summary>
public static class ActorNotice
{
    public static async Task<string?> NameAsync(
        IActorLookupService actors,
        IWorkspaceAccessResolver access,
        Guid workspaceId,
        Guid viewerId,
        Guid? actorId,
        CancellationToken ct)
    {
        if (actorId is not { } id) return null;
        if ((await access.ResolveAsync(workspaceId, viewerId, ct)).IsGuest) return null;
        return (await actors.FindManyAsync([id], ct)).FirstOrDefault()?.Name;
    }
}
