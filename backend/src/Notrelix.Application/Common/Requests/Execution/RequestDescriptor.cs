namespace Notrelix.Application.Common.Requests.Execution;

/// <param name="ApiTokenScope">
/// The one scope an API token must carry to send this request (see <c>ApiTokenScopeMap</c>); null when no token
/// may send it. Meaningful only for authenticated workspace and resource requests: a token reaches nothing else.
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
    string? ApiTokenScope = null);
