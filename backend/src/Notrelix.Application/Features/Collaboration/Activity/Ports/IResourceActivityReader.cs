namespace Notrelix.Application.Features.Collaboration.Activity.Ports;

/// <summary>
/// Reads the activity rows Collaboration recorded for one resource. The projection lives in
/// Infrastructure; the use case sees only this port. The caller is already authorized to see the resource.
/// </summary>
public interface IResourceActivityReader
{
    /// <summary>One page of the resource's history, newest first (ties newest row first). Pages start at 1.</summary>
    Task<ResourceActivityPage> ListAsync(
        Guid workspaceId,
        string resourceKind,
        Guid resourceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public sealed record ResourceActivityPage(IReadOnlyList<ResourceActivityRow> Rows, int Total);

/// <param name="ActivityType">The stable type of the fact, e.g. <c>work.item-status-changed</c>.</param>
/// <param name="Title">An English phrase for the fact, for clients that do not know the type.</param>
/// <param name="DataJson">The small language-neutral detail the type carries.</param>
public sealed record ResourceActivityRow(
    Guid Id,
    Guid? ActorUserId,
    string ActivityType,
    string Title,
    string? ResourceTitle,
    string DataJson,
    DateTimeOffset OccurredAt);
