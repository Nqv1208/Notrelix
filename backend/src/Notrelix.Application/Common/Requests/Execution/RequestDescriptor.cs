namespace Notrelix.Application.Common.Requests.Execution;

public sealed record RequestDescriptor(
    Type RequestType,
    ApplicationRequestKind Kind,
    ApplicationPrincipalKind Principal,
    ApplicationScopeKind Scope,
    ApplicationDataAccessKind DataAccess,
    AccessRequirements Access,
    bool IsIdempotent,
    bool RequiresExpectedVersion);
