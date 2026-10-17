namespace Notrelix.Application.Features.Governance.Authorization;

/// <summary>Which source gives a principal its effective level on a Board (GOV-025 order).</summary>
public enum EffectiveAccessBasis
{
    /// <summary>The single read (<c>ViewBoard</c>) is not allowed.</summary>
    None,

    /// <summary>Workspace Owner (GOV-025 step 2).</summary>
    WorkspaceOwner,

    /// <summary>An explicit resource permission to the User or one of its active Teams.</summary>
    ResourcePermission,

    /// <summary>Board membership without an explicit resource permission.</summary>
    BoardMembership,

    /// <summary>Workspace Admin on a Workspace-visible Board placed in a Space (Space Admin everywhere).</summary>
    WorkspaceAdmin,

    /// <summary>The Space level on a Workspace-visible Board placed in a Space (GOV-027).</summary>
    Space,

    /// <summary>Workspace default on an unplaced Workspace-visible Board (GOV-025 step 6).</summary>
    WorkspaceDefault,

    /// <summary>Only an explicit permission rule allows the read (GOV-025 step 3).</summary>
    PermissionRule,
}

/// <summary>
/// A principal's effective level on a Board and the source that gives it.
/// <see cref="Rank"/> uses the Governance rank order (Viewer=1, Commenter=2,
/// Editor=3, Manager=4, Owner=5); <see cref="Level"/> names it for clients,
/// with Owner reported as Manager.
/// </summary>
public readonly record struct EffectiveResourceAccess(int Rank, EffectiveAccessBasis Basis)
{
    public static readonly EffectiveResourceAccess None = new(0, EffectiveAccessBasis.None);

    public string? Level => Basis == EffectiveAccessBasis.None
        ? null
        : Rank switch
        {
            >= 4 => "Manager",
            3 => "Editor",
            2 => "Commenter",
            _ => "Viewer",
        };
}
