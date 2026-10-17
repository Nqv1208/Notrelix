namespace Notrelix.Application.Features.Governance.Public.Access;

/// <summary>
/// The level each board membership role gives on its Board, as a Governance
/// rank (Viewer=1, Commenter=2, Editor=3, Manager=4); the single mapping table
/// used by the access engine and by requests that assign board roles (the
/// role's rank is the grant they request). Owner and Admin manage the Board's
/// access, Member edits, Observer and Guest read. No role → 0; an unknown role
/// reads only.
/// </summary>
public static class BoardRoleLevels
{
    public static int RankOf(string? boardRole) => boardRole switch
    {
        null => 0,
        "Owner" or "Admin" => 4,
        "Member" => 3,
        "Observer" or "Guest" => 1,
        _ => 1,
    };
}
