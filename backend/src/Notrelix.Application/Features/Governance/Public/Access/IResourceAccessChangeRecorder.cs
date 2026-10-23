namespace Notrelix.Application.Features.Governance.Public.Access;

/// <summary>
/// Governance-published action that records a change of who can reach a resource, for the sharing log
/// that managers read (<see cref="IResourceAccessSummary.GetBoardSharingAsync"/>). The owning context
/// calls it inside the same request transaction as the change itself, so the record exists exactly when
/// the change does. Records are kept for 90 days.
/// </summary>
public interface IResourceAccessChangeRecorder
{
    Task RecordAsync(ResourceAccessChangeRecord change, CancellationToken cancellationToken);
}

/// <param name="ResourceKind">Canonical kind, for example <c>work-management.board</c>.</param>
/// <param name="Change">One of <see cref="AccessChangeKinds"/>.</param>
/// <param name="SubjectType"><c>User</c> or <c>Team</c> for a subject change; null for a visibility change.</param>
/// <param name="FromLevel">One of <see cref="AccessLevels"/> before the change, or null.</param>
/// <param name="ToLevel">One of <see cref="AccessLevels"/> after the change, or null.</param>
public sealed record ResourceAccessChangeRecord(
    string ResourceKind,
    Guid ResourceId,
    string Change,
    string? SubjectType,
    Guid? SubjectId,
    string? FromLevel,
    string? ToLevel);

public static class AccessChangeKinds
{
    public const string Added = "added";
    public const string Changed = "changed";
    public const string Removed = "removed";
    public const string MadePrivate = "private";
    public const string VisibilityChanged = "visibility";
}
