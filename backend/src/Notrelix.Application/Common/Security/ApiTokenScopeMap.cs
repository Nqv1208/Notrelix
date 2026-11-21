using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Common.Security;

/// <summary>
/// The one mapping from a request to the API token scope it needs (read into <see cref="RequestDescriptor.ApiTokenScope"/>
/// when the descriptor is built). A scope only narrows access: the issuer's own permissions still apply afterwards.
/// <list type="bullet">
/// <item>The context is the feature namespace, <c>Notrelix.Application.Features.&lt;Context&gt;</c>:
/// WorkManagement is <c>work</c>, Documents is <c>docs</c>, Chat is <c>chat</c>.</item>
/// <item>A query that only reads needs <c>:read</c>; anything else, a command or a query that writes, needs
/// <c>:write</c>. Write does not imply read.</item>
/// <item>Every other context is refused to every token (<see cref="RefusedContexts"/>), except the named
/// <see cref="WorkspaceReadRequests"/>, which need <c>workspace:read</c>.</item>
/// </list>
/// A request that maps to no scope (null) is refused to every token, so a context added later reaches no token
/// until it is mapped here; the architecture test names every context of the workspace-scoped requests.
/// </summary>
public static class ApiTokenScopeMap
{
    private const string FeaturesNamespace = "Notrelix.Application.Features.";

    /// <summary>Feature context to the scope prefix of its read and write scopes.</summary>
    public static readonly IReadOnlyDictionary<string, string> ScopedContexts = new Dictionary<string, string>
    {
        ["WorkManagement"] = "work",
        ["Documents"] = "docs",
        ["Chat"] = "chat",
    };

    /// <summary>
    /// Contexts a token never reaches. Stricter answer: they hold people, roles, billing, integrations, analytics and
    /// cross-cutting resources (comments, attachments) whose scope would depend on the resource they sit on.
    /// </summary>
    public static readonly IReadOnlySet<string> RefusedContexts = new HashSet<string>(StringComparer.Ordinal)
    {
        "Accounts", "Analytics", "Automation", "Billing", "Collaboration", "Governance", "Identity",
        "Integrations", "Notifications", "Operators", "Workspaces",
    };

    /// <summary>
    /// The defined read set of <see cref="ApiTokenScopes.WorkspaceRead"/>: the workspace itself, its spaces and its teams.
    /// No member, invitation, setting, role, plan or token data.
    /// </summary>
    public static readonly IReadOnlySet<string> WorkspaceReadRequests = new HashSet<string>(StringComparer.Ordinal)
    {
        "Notrelix.Application.Features.Workspaces.Workspaces.Queries.GetWorkspace.GetWorkspaceQuery",
        "Notrelix.Application.Features.Workspaces.Spaces.Queries.GetWorkspaceSpaces.GetWorkspaceSpacesQuery",
        "Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpace.GetSpaceQuery",
        "Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceTree.GetSpaceTreeQuery",
        "Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams.GetWorkspaceTeamsQuery",
        "Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam.GetTeamQuery",
    };

    /// <summary>The feature context of a request type, or null for a type outside the feature namespaces.</summary>
    public static string? ContextOf(Type requestType)
    {
        var ns = requestType.Namespace;
        if (ns is null || !ns.StartsWith(FeaturesNamespace, StringComparison.Ordinal))
        {
            return null;
        }

        var rest = ns[FeaturesNamespace.Length..];
        var dot = rest.IndexOf('.');
        return dot < 0 ? rest : rest[..dot];
    }

    /// <summary>The scope a token needs to send the request; null when no token may send it.</summary>
    public static string? RequiredScope(
        Type requestType,
        ApplicationRequestKind kind,
        ApplicationDataAccessKind dataAccess)
    {
        if (WorkspaceReadRequests.Contains(requestType.FullName ?? string.Empty))
        {
            return ApiTokenScopes.WorkspaceRead;
        }

        if (ContextOf(requestType) is not { } context || !ScopedContexts.TryGetValue(context, out var prefix))
        {
            return null;
        }

        var reads = kind == ApplicationRequestKind.Query && dataAccess == ApplicationDataAccessKind.Read;
        return $"{prefix}:{(reads ? "read" : "write")}";
    }
}
