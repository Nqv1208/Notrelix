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
/// <item>A named <see cref="ResourceHostedRequests"/> request of Collaboration (a comment, attachment or reaction) takes the
/// scope of the resource it sits on, read from the kind of the resource the request names
/// (<see cref="ResourceKindContexts"/>): a board item is <c>work</c>, a page is <c>docs</c>. The kind is the request's own
/// value, so nothing is read from the database; any other kind, and every Collaboration request that does not name its host
/// resource, stays refused.</item>
/// <item>A named <see cref="LocatedHostRequests"/> request (edit, resolve, reopen or delete a comment, delete an item
/// attachment) names only a comment or attachment id: the resource locator, which already runs before the scope decision to
/// answer a missing id with NotFound, also returns the kind of the resource it sits on, and that kind decides the scope as
/// above. The decision itself reads nothing.</item>
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
    /// Collaboration requests that name the resource they sit on (a board item or a page) in their own <c>Resource</c>:
    /// their scope follows that resource's kind (<see cref="RequiredScopeForResource"/>). A Collaboration request not
    /// listed here stays refused, among them those keyed by a comment or attachment id (the host is not in the request),
    /// activity, watchers, notifications and preferences.
    /// </summary>
    public static readonly IReadOnlySet<string> ResourceHostedRequests = new HashSet<string>(StringComparer.Ordinal)
    {
        "Notrelix.Application.Features.Collaboration.Comments.Commands.CreateComment.CreateCommentCommand",
        "Notrelix.Application.Features.Collaboration.Comments.Queries.GetComments.GetCommentsQuery",
        "Notrelix.Application.Features.Collaboration.Reactions.Commands.AddReaction.AddReactionCommand",
        "Notrelix.Application.Features.Collaboration.Reactions.Commands.RemoveReaction.RemoveReactionCommand",
        "Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachments.GetBoardItemAttachmentsQuery",
        "Notrelix.Application.Features.Collaboration.Attachments.Queries.GetBoardItemAttachmentContent.GetBoardItemAttachmentContentQuery",
        "Notrelix.Application.Features.Collaboration.Attachments.Commands.CreateBoardItemAttachment.CreateBoardItemAttachmentCommand",
        "Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadBoardItemAttachment.UploadBoardItemAttachmentCommand",
        "Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachments.GetPageAttachmentsQuery",
        "Notrelix.Application.Features.Collaboration.Attachments.Queries.GetPageAttachmentContent.GetPageAttachmentContentQuery",
        "Notrelix.Application.Features.Collaboration.Attachments.Commands.CreatePageAttachment.CreatePageAttachmentCommand",
        "Notrelix.Application.Features.Collaboration.Attachments.Commands.UploadPageAttachment.UploadPageAttachmentCommand",
        "Notrelix.Application.Features.Collaboration.Attachments.Commands.DeletePageAttachment.DeletePageAttachmentCommand",
    };

    /// <summary>
    /// Collaboration requests keyed by a comment or attachment id: their scope follows the host kind the resource locator
    /// returns for that id (<see cref="ResourceLocation.HostKind"/>). The value is the one host kind the request accepts, or
    /// null for any kind <see cref="ResourceKindContexts"/> maps. A host of another kind than the required one is as missing as
    /// a made-up id (the handler answers the same NotFound); an unmapped host kind is refused.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string?> LocatedHostRequests = new Dictionary<string, string?>
    {
        ["Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment.UpdateCommentCommand"] = null,
        ["Notrelix.Application.Features.Collaboration.Comments.Commands.DeleteComment.DeleteCommentCommand"] = null,
        ["Notrelix.Application.Features.Collaboration.Comments.Commands.ResolveComment.ResolveCommentCommand"] = null,
        ["Notrelix.Application.Features.Collaboration.Comments.Commands.ReopenComment.ReopenCommentCommand"] = null,
        ["Notrelix.Application.Features.Collaboration.Attachments.Commands.DeleteAttachment.DeleteAttachmentCommand"] = "work-management.board-item",
    };

    /// <summary>Resource kind (exact value) to the scope prefix of the context that owns it; any other kind is refused.</summary>
    public static readonly IReadOnlyDictionary<string, string> ResourceKindContexts = new Dictionary<string, string>
    {
        ["work-management.board-item"] = "work",
        ["documents.page"] = "docs",
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

        return Scope(prefix, kind, dataAccess);
    }

    /// <summary>
    /// The scope a token needs for a <see cref="ResourceHostedRequests"/> request on a resource of this kind; null (refused)
    /// for a kind no context owns here. Decided from the kind alone, never from the database.
    /// </summary>
    public static string? RequiredScopeForResource(
        ResourceKind resourceKind,
        ApplicationRequestKind kind,
        ApplicationDataAccessKind dataAccess) =>
        ResourceKindContexts.TryGetValue(resourceKind.Value, out var prefix) ? Scope(prefix, kind, dataAccess) : null;

    private static string Scope(string prefix, ApplicationRequestKind kind, ApplicationDataAccessKind dataAccess) =>
        $"{prefix}:{(kind == ApplicationRequestKind.Query && dataAccess == ApplicationDataAccessKind.Read ? "read" : "write")}";
}
