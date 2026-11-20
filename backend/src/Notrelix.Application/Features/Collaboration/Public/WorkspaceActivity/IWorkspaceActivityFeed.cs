namespace Notrelix.Application.Features.Collaboration.Public.WorkspaceActivity;

/// <summary>
/// Collaboration-owned boundary for the workspace activity feed (docs/product/workspaces.md section 81, LD-15): the facts
/// Collaboration already recorded for a Workspace, newest first, one keyset batch at a time. The feed does not decide who may
/// read a fact. Each fact says which resource it is about (<see cref="WorkspaceActivityScope"/>), and the caller drops the ones
/// whose resource its reader cannot see. Activity is not Audit (COL-020): Governance keeps the audit evidence.
/// </summary>
public interface IWorkspaceActivityFeed
{
    /// <summary>The facts strictly older than <paramref name="after"/> (all when null), newest first, at most <paramref name="take"/>.</summary>
    Task<IReadOnlyList<WorkspaceActivityFact>> ListAsync(
        Guid workspaceId, WorkspaceActivityPosition? after, int take, CancellationToken cancellationToken);
}

/// <summary>A place in the feed: the time of a fact and its id, which breaks ties.</summary>
public readonly record struct WorkspaceActivityPosition(DateTimeOffset OccurredAt, Guid Id);

public enum WorkspaceActivityScopeKind
{
    /// <summary>About the workspace itself (a member joined); every member may read it.</summary>
    Workspace,

    /// <summary>About a Board, or an Item on it; readable when the Board is.</summary>
    Board,

    /// <summary>About a Page; readable when the Page is.</summary>
    Page,

    /// <summary>The fact does not say which Board or Page it is about, so no reader may be shown it.</summary>
    Unresolved,
}

public sealed record WorkspaceActivityScope(WorkspaceActivityScopeKind Kind, Guid? ResourceId);

/// <param name="Type">The stable type of the fact, e.g. <c>work.board-created</c>; a client words it in its own language.</param>
/// <param name="Title">An English phrase for clients that do not know <paramref name="Type"/>.</param>
public sealed record WorkspaceActivityFact(
    Guid Id,
    string Type,
    Guid? ActorUserId,
    string Title,
    string SubjectType,
    Guid SubjectId,
    string? SubjectName,
    WorkspaceActivityScope Scope,
    DateTimeOffset OccurredAt)
{
    public WorkspaceActivityPosition Position => new(OccurredAt, Id);
}
