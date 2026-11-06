namespace Notrelix.Application.Features.WorkManagement.Ordering;

/// <summary>
/// A move whose neighbours are no longer adjacent lost a race with another writer of the same ordering scope. The
/// 409 names who, as the 412 of an item or a view does: the display name only, none for a Workspace Guest or for a
/// person the caller has no workspace relationship to. Best effort: any failure leaves the plain conflict.
/// </summary>
public static class StalePlacementNotice
{
    public static async Task<ConflictException> NameAsync(
        ConflictException stale,
        Guid? lastWriter,
        ICurrentRequestContext request,
        IActorLookupService? actors,
        IWorkspaceAccessResolver? access,
        CancellationToken ct)
    {
        if (actors is null || access is null || lastWriter is null) return stale;
        try
        {
            var name = await ActorNotice.NameAsync(actors, access, request.RequireWorkspaceId(), request.UserId, lastWriter, ct);
            return new ConflictException(stale.Message, stale.ErrorCode!)
            {
                Extensions = new Dictionary<string, object?> { ["changedByName"] = name },
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return stale;
        }
    }
}
