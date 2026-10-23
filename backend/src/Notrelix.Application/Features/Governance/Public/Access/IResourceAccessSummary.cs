namespace Notrelix.Application.Features.Governance.Public.Access;

/// <summary>
/// Governance-published access summary of one Board for the calling principal:
/// the caller's effective level and its source, and who else can reach the
/// Board, counted from the explicit sources Governance can enumerate. Derived
/// state for clients; the server still authorizes every operation.
/// </summary>
/// <remarks>
/// The calling principal and Workspace come from the current execution
/// context. Governance resolves the canonical access facts of a <c>ViewBoard</c>
/// read of the Board and evaluates the single-read decision again, so a
/// principal without access gets <c>null</c> (the caller maps it to the same
/// NotFound as a missing Board).
/// Names are disclosed only where the caller can already see them: board
/// members (the Board's member list), the Space the caller can see, and Team or
/// User grants only for a caller who may manage the Board's access; everything
/// else is reported as a count.
/// </remarks>
public interface IResourceAccessSummary
{
    Task<BoardAccessSummary?> GetBoardAccessAsync(Guid boardId, CancellationToken cancellationToken);
}

/// <param name="MyLevel">One of <see cref="AccessLevels"/>.</param>
/// <param name="PeopleCount">Distinct people reached by <paramref name="Sources"/>, Guests included.</param>
/// <param name="GuestCount">Of <paramref name="PeopleCount"/>, the Workspace Guests.</param>
/// <param name="PeopleCountComplete">
/// True only when the counted sources are provably everyone who can open the
/// Board: a Private Board with no applicable read permission rule. False when
/// implicit reach exists that is not enumerated (Space inheritance, open
/// Spaces, workspace Admins, the Workspace default, permission rules).
/// </param>
/// <param name="Sources">Each counted person appears in exactly one source.</param>
public sealed record BoardAccessSummary(
    string MyLevel,
    AccessSource MySource,
    int PeopleCount,
    int GuestCount,
    bool PeopleCountComplete,
    IReadOnlyList<AccessSourceCount> Sources);

/// <param name="Kind">One of <see cref="AccessSourceKinds"/>.</param>
/// <param name="GrantedBy">Who created the caller's explicit or Team grant, when known.</param>
public sealed record AccessSource(string Kind, AccessGrantor? GrantedBy);

public sealed record AccessGrantor(Guid UserId, string Name);

/// <param name="Kind"><c>space</c>, <c>team</c>, <c>user</c> or <c>guest</c>.</param>
/// <param name="Id">Present only when the caller may see the subject.</param>
/// <param name="Name">Present only when the caller may see the subject.</param>
public sealed record AccessSourceCount(string Kind, Guid? Id, string? Name, int Count);

public static class AccessLevels
{
    public const string Viewer = "viewer";
    public const string Commenter = "commenter";
    public const string Editor = "editor";
    public const string Manager = "manager";

    /// <summary>The level token of a Governance rank (Viewer=1, Commenter=2, Editor=3, Manager and above=4+); 0 or less reads as viewer.</summary>
    public static string OfRank(int rank) => rank switch
    {
        >= 4 => Manager,
        3 => Editor,
        2 => Commenter,
        _ => Viewer,
    };
}

public static class AccessSourceKinds
{
    /// <summary>Workspace Owner.</summary>
    public const string Owner = "owner";

    /// <summary>Workspace Admin through the Board's Space.</summary>
    public const string WorkspaceAdmin = "workspace-admin";

    /// <summary>Space level (caller's source) or the Space's direct members (count source).</summary>
    public const string Space = "space";

    /// <summary>A grant to a Team the caller is an active member of, or a granted Team (count source).</summary>
    public const string Team = "team";

    /// <summary>A grant to the caller, board membership, or an explicit permission rule.</summary>
    public const string Explicit = "explicit";

    /// <summary>The caller is a Workspace Guest (caller's source) or the Guests reached (count source).</summary>
    public const string Guest = "guest";

    /// <summary>The Workspace default of an unplaced Workspace-visible Board.</summary>
    public const string Workspace = "workspace";

    /// <summary>A board member, a User grant or a workspace Owner (count source).</summary>
    public const string User = "user";
}
