namespace Notrelix.Application.Features.WorkManagement.Common.DTOs;

/// <summary>
/// The caller's access to a Board and who else can reach it, as decided by
/// Governance. Derived state for clients; every operation is still authorized.
/// </summary>
/// <param name="Version">The Board's version when the summary was read.</param>
/// <param name="MyLevel"><c>viewer</c>, <c>commenter</c>, <c>editor</c> or <c>manager</c>.</param>
/// <param name="PeopleCount">
/// Distinct people who can open the Board through the counted sources, Guests
/// included (the sum of <paramref name="Sources"/> counts). Counted sources are
/// explicit: the Space's direct User/Team entries (Workspace-visible Board
/// placed in a Space), granted Teams' active members, User grants, board
/// members and workspace Owners. Workspace Admins, inherited ancestor-Space
/// entries, permission rules and the open-Space / unplaced-Board Workspace
/// defaults are not enumerated; see <paramref name="PeopleCountComplete"/>.
/// </param>
/// <param name="GuestCount">Of <paramref name="PeopleCount"/>, the Workspace Guests.</param>
/// <param name="PeopleCountComplete">
/// True only when <paramref name="PeopleCount"/> is provably everyone who can
/// open the Board (a Private Board with no applicable read permission rule);
/// false whenever implicit reach exists that is not enumerated.
/// </param>
/// <param name="Sources">Each counted person appears in exactly one source, in the order Space, Team, User, Guest.</param>
public record BoardAccessDto(
    Guid BoardId,
    long Version,
    string MyLevel,
    BoardAccessSourceDto MySource,
    int PeopleCount,
    int GuestCount,
    bool PeopleCountComplete,
    List<BoardAccessSourceCountDto> Sources);

/// <param name="Kind">
/// <c>owner</c> (workspace Owner), <c>workspace-admin</c> (workspace Admin
/// through the Board's Space), <c>space</c> (Space level), <c>team</c> (a grant
/// to the caller's Team), <c>explicit</c> (a grant to the caller, board
/// membership or a permission rule), <c>guest</c> (the caller is a workspace
/// Guest) or <c>workspace</c> (the Workspace default of an unplaced
/// Workspace-visible Board).
/// </param>
/// <param name="GrantedBy">Who created the caller's grant, for <c>explicit</c>, <c>team</c> or <c>guest</c> when known.</param>
public record BoardAccessSourceDto(string Kind, BoardAccessGrantorDto? GrantedBy);

public record BoardAccessGrantorDto(Guid UserId, string Name);

/// <param name="Kind"><c>space</c>, <c>team</c>, <c>user</c> or <c>guest</c>.</param>
/// <param name="Id">Present only when the caller may see the subject.</param>
/// <param name="Name">Present only when the caller may see the subject.</param>
public record BoardAccessSourceCountDto(string Kind, Guid? Id, string? Name, int Count);
