namespace Notrelix.Application.Common.Requests.Execution;

/// <param name="ApiTokenScope">
/// The one scope an API token must carry to send this request (see <c>ApiTokenScopeMap</c>); null when no token
/// may send it. Meaningful only for authenticated workspace and resource requests: a token reaches nothing else.
/// </param>
/// <param name="ApiTokenScopeFollowsResource">
/// True for a request whose token scope is decided by the kind of the resource it names (a Collaboration comment,
/// attachment or reaction on a board item or page); <c>ApiTokenScope</c> is then null. The behavior reads the kind
/// from the request's own resource, with no database read.
/// </param>
/// <param name="ApiTokenScopeFollowsLocatedHost">
/// True for a request keyed by a comment or attachment id (<c>ApiTokenScopeMap.LocatedHostRequests</c>): the token scope is
/// decided by the host kind the resource locator returned for that id, in the same call that answers a missing id with
/// NotFound; <c>ApiTokenScope</c> is then null.
/// </param>
public sealed record RequestDescriptor(
    Type RequestType,
    ApplicationRequestKind Kind,
    ApplicationPrincipalKind Principal,
    ApplicationScopeKind Scope,
    ApplicationDataAccessKind DataAccess,
    AccessRequirements Access,
    bool IsIdempotent,
    bool RequiresExpectedVersion,
    string? ApiTokenScope = null,
    bool ApiTokenScopeFollowsResource = false,
    bool ApiTokenScopeFollowsLocatedHost = false);
