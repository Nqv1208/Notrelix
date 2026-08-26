using Notrelix.Application.Common.Requests.Execution;

namespace Notrelix.Application.Common.Context;

public sealed record ExecutionContextSnapshot(
    Guid? UserId,
    Guid? AccountId,
    Guid? WorkspaceId,
    ResourceRef? Resource,
    ApplicationPrincipalKind Principal,
    ApplicationScopeKind Scope,
    string CorrelationId);
