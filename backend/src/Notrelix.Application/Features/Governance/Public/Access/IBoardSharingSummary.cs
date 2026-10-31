namespace Notrelix.Application.Features.Governance.Public.Access;

/// <summary>
/// Governance-published sharing view of one Board for the calling principal (CTR-GAP-WM-BOARD-SHARING):
/// who holds access given on the Board itself, the sharing log, and who would lose the Board if it went
/// Private. Names and levels are disclosed only to a caller who may manage the Board's access or is a
/// member of the Board (the disclosure rule of <see cref="IResourceAccessSummary"/>); every other caller
/// who can read the Board gets the counts only.
/// </summary>
/// <remarks>
/// A caller who cannot read the Board gets <c>null</c> (the same NotFound as a missing Board). The caller
/// and Workspace come from the execution context. Work Management owns the Board, its members, its
/// visibility and its Items; it consumes this fact and adds the Item figures itself.
/// </remarks>
public interface IBoardSharingSummary
{
    Task<BoardSharing?> GetBoardSharingAsync(Guid boardId, CancellationToken cancellationToken);

    /// <summary>
    /// Who would gain and who would lose the Board if it were placed in <paramref name="targetSpaceId"/> (null =
    /// the Workspace root). Null unless the caller manages the Board, so a caller without that right learns
    /// nothing, exactly as for a missing Board. <paramref name="candidateUserIds"/> are the people whose fate the
    /// caller needs when the Workspace's members are not enumerated (a Board at the root).
    /// </summary>
    Task<BoardPlacementReach?> GetBoardPlacementReachAsync(Guid boardId, Guid? targetSpaceId, IReadOnlyCollection<Guid> candidateUserIds, CancellationToken cancellationToken);
}

/// <param name="Everyone">The target is the Workspace root, which every member who is not a Guest reaches, so no list of who gains is kept.</param>
/// <param name="EveryoneLoses">The Board sits at the Workspace root today, so every member not reached by the target or by an entry of the Board loses it; only the candidates are listed.</param>
/// <param name="TargetSpaceName">Name of the target Space.</param>
/// <param name="TargetPeople">People the target Space reaches (Guests excluded).</param>
/// <param name="LosingCount">People who stop reaching the Board; null when <paramref name="EveryoneLoses"/> (the Workspace's members are not enumerated).</param>
/// <param name="LosingUserIds">The people losing the Board; with <paramref name="EveryoneLoses"/> only those among the candidates.</param>
/// <param name="GainingCount">People only the target reaches.</param>
/// <param name="KeptUserIds">People who keep access on their own entry (Board member or explicit grant).</param>
/// <param name="KeptTeamCount">Teams granted on the Board itself.</param>
public sealed record BoardPlacementReach(
    bool Everyone,
    bool EveryoneLoses,
    string? TargetSpaceName,
    int TargetPeople,
    int? LosingCount,
    IReadOnlyList<Guid> LosingUserIds,
    int GainingCount,
    IReadOnlyList<Guid> KeptUserIds,
    int KeptTeamCount);

/// <param name="CanManage">The caller may manage the Board's access (and so see the log and the Private impact).</param>
/// <param name="EntriesDisclosed">Entries are listed: the caller manages the Board or is one of its members.</param>
/// <param name="Visibility">The Board's visibility: <c>Private</c>, <c>Workspace</c> or <c>PublicLink</c>.</param>
/// <param name="SpaceId">The Space whose direct entries reach the Board, when it is placed and Workspace-visible.</param>
/// <param name="SpaceName">Present only when the caller can see the Space.</param>
/// <param name="SpacePeople">People the Space's entries reach (Guests excluded); present only when the caller can see the Space.</param>
/// <param name="Entries">One per person or Team given access on the Board itself; empty unless <paramref name="EntriesDisclosed"/>.</param>
/// <param name="Log">Newest first; empty unless <paramref name="CanManage"/>.</param>
/// <param name="LosingUserIds">
/// People who reach the Board only through its Space and so lose it when it goes Private; empty unless
/// <paramref name="CanManage"/>.
/// </param>
public sealed record BoardSharing(
    bool CanManage,
    bool EntriesDisclosed,
    string Visibility,
    Guid? SpaceId,
    string? SpaceName,
    int? SpacePeople,
    int PeopleCount,
    int GuestCount,
    bool PeopleCountComplete,
    IReadOnlyList<BoardSharingEntry> Entries,
    IReadOnlyList<BoardSharingLogEntry> Log,
    IReadOnlyList<Guid> LosingUserIds);

/// <param name="Kind"><c>person</c>, <c>team</c> or <c>guest</c> (a person whose Workspace role is Guest; a membership fact, never an email domain).</param>
/// <param name="Id">The person's user id or the Team's id: the key the existing writes use.</param>
/// <param name="Level">One of <see cref="AccessLevels"/>: the higher of the explicit grant and the board-member level; a Guest is capped at Editor.</param>
/// <param name="LevelCapped">The stored level is higher than <paramref name="Level"/> because a Guest is capped (GOV-030).</param>
/// <param name="Source"><c>grant</c>, <c>member</c> or <c>both</c>.</param>
/// <param name="PermissionId">The explicit grant to revoke, when there is one.</param>
/// <param name="MemberRole">The Board role name, when the person is a Board member.</param>
/// <param name="Owner">The person holds the Owner role of the Board.</param>
/// <param name="SpaceRole"><c>Admin</c>, <c>Member</c> or <c>Viewer</c> when the Space reaches the person.</param>
/// <param name="OutsideSpace">The Board is placed in a Space and the Space does not reach the person.</param>
/// <param name="TeamPeople">Active members of a Team entry.</param>
public sealed record BoardSharingEntry(
    string Kind,
    Guid Id,
    string Name,
    string Level,
    bool LevelCapped,
    string Source,
    Guid? PermissionId,
    string? MemberRole,
    bool Owner,
    bool You,
    string? SpaceRole,
    bool OutsideSpace,
    int? TeamPeople,
    DateTimeOffset AddedAt,
    AccessGrantor? GrantedBy);

/// <param name="Kind">One of <see cref="AccessChangeKinds"/>.</param>
/// <param name="Target">The person's or Team's name, or null for a visibility change.</param>
public sealed record BoardSharingLogEntry(
    Guid Id,
    string Kind,
    AccessGrantor? Actor,
    string? Target,
    string? From,
    string? To,
    bool Guest,
    DateTimeOffset At);

public static class BoardSharingEntryKinds
{
    public const string Person = "person";
    public const string Team = "team";
    public const string Guest = "guest";
}

public static class BoardSharingEntrySources
{
    public const string Grant = "grant";
    public const string Member = "member";
    public const string Both = "both";
}
