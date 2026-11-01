namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

/// <summary>
/// The Board's sharing view (CTR-GAP-WM-BOARD-SHARING). A caller who manages the Board or is one of its
/// members gets <paramref name="Entries"/> (people and Teams with their level and the way it is held); any
/// other caller who can read the Board gets the counts only and an empty list. The log and the Private
/// impact are for Board managers only.
/// </summary>
/// <param name="Visibility"><c>Private</c>, <c>Workspace</c> or <c>PublicLink</c>.</param>
/// <param name="SpaceName">Present only when the caller can see the Space.</param>
/// <param name="SpacePeople">People the Space's entries reach (Guests excluded); present only when the caller can see the Space.</param>
/// <param name="PrivateImpact">Null unless the caller manages the Board.</param>
public record BoardSharingDto(
    Guid BoardId,
    bool CanManage,
    bool EntriesDisclosed,
    string Visibility,
    Guid? SpaceId,
    string? SpaceName,
    int? SpacePeople,
    int PeopleCount,
    int GuestCount,
    bool PeopleCountComplete,
    IReadOnlyList<BoardSharingEntryDto> Entries,
    IReadOnlyList<BoardSharingLogEntryDto> Log,
    BoardPrivateImpactDto? PrivateImpact);

/// <param name="Id">The person's user id or the Team's id: the key of the existing writes.</param>
/// <param name="Kind"><c>person</c>, <c>team</c> or <c>guest</c> (Workspace role Guest; a membership fact, never an email domain).</param>
/// <param name="Email">Only for a Workspace Owner or Admin reading a non-Guest person; null otherwise.</param>
/// <param name="Level"><c>viewer</c>, <c>commenter</c>, <c>editor</c> or <c>manager</c>; the higher of the explicit grant and the board-member level, a Guest capped at Editor (GOV-030).</param>
/// <param name="LevelCapped">The stored level is above <paramref name="Level"/> because the person is a Guest.</param>
/// <param name="Source"><c>grant</c>, <c>member</c> or <c>both</c>.</param>
/// <param name="PermissionId">The explicit grant to revoke, when there is one.</param>
/// <param name="PermissionVersion">The version of that grant, which a change of its level must name.</param>
/// <param name="MemberRole">The Board role, when the person is a Board member.</param>
/// <param name="ExpiresAt">Always null: access given on a Board has no end date.</param>
public record BoardSharingEntryDto(
    Guid Id,
    string Kind,
    string Name,
    string? Email,
    bool You,
    bool Owner,
    string Level,
    bool LevelCapped,
    string Source,
    Guid? PermissionId,
    long? PermissionVersion,
    string? MemberRole,
    string? SpaceRole,
    bool OutsideSpace,
    int? TeamPeople,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset AddedAt,
    BoardAccessGrantorDto? GrantedBy);

/// <param name="Kind"><c>added</c>, <c>changed</c>, <c>removed</c>, <c>private</c> or <c>visibility</c>; never <c>expired</c>.</param>
/// <param name="Target">The person's or Team's name; null for a visibility change.</param>
public record BoardSharingLogEntryDto(
    Guid Id,
    string Kind,
    BoardAccessGrantorDto? Actor,
    string? Target,
    string? From,
    string? To,
    bool Guest,
    DateTimeOffset At);

/// <param name="Lose">People who reach the Board only through its Space and would lose it.</param>
/// <param name="People">Those of them with unfinished Items on the Board.</param>
/// <param name="Keep">Names of the entries that keep their access.</param>
public record BoardPrivateImpactDto(
    int Lose,
    IReadOnlyList<BoardPrivateImpactPersonDto> People,
    IReadOnlyList<string> Keep);

/// <param name="Items">Unfinished (not completed, not archived) Items of the Board assigned to the person.</param>
/// <param name="Overdue">Of <paramref name="Items"/>, those past their due instant.</param>
public record BoardPrivateImpactPersonDto(Guid UserId, string Name, int Items, int Overdue);
