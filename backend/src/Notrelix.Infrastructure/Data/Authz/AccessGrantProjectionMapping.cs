using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Infrastructure.Data.Authz;

/// <summary>
/// Maps membership roles to the access-grant projection columns read by RLS
/// helpers (ops.has_account_access / ops.has_workspace_access).
/// </summary>
public static class AccessGrantProjectionMapping
{
    public static string[] RoleCodes(AccountRole role) => [role.ToString()];

    public static string[] RoleCodes(WorkspaceRole role) => [role.ToString()];

    public static bool IsAccountAdmin(AccountRole role) => role is AccountRole.Owner or AccountRole.Admin;

    public static bool IsWorkspaceAdmin(WorkspaceRole role) => role is WorkspaceRole.Owner or WorkspaceRole.Admin;
}