namespace Notrelix.Application.Features.Governance.Abstractions;

/// <summary>
/// Reads the retained access changes of one resource, newest first, for the sharing log. Executed on the
/// request's data session, so tenant RLS applies. A Team subject is named here (the name it has now); a
/// person is named by the caller.
/// </summary>
public interface IResourceAccessChangeReader
{
    Task<IReadOnlyList<ResourceAccessChangeRow>> ReadAsync(
        Guid workspaceId,
        string resourceKind,
        Guid resourceId,
        DateTimeOffset now,
        int limit,
        CancellationToken cancellationToken);
}

/// <param name="Change">One of <see cref="Public.Access.AccessChangeKinds"/>.</param>
/// <param name="SubjectName">The Team's name for a Team subject; null otherwise.</param>
/// <param name="SubjectIsGuest">The person subject is currently a Workspace Guest.</param>
public sealed record ResourceAccessChangeRow(
    Guid Id,
    string Change,
    Guid ActorUserId,
    string? SubjectType,
    Guid? SubjectId,
    string? SubjectName,
    bool SubjectIsGuest,
    string? FromLevel,
    string? ToLevel,
    DateTimeOffset OccurredAt);
