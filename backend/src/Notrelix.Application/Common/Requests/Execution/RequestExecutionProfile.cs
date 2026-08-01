namespace Notrelix.Application.Common.Requests.Execution;

/// <summary>
/// Categorical execution profile for an Application request.
/// Every request has exactly one value per category (Kind, Principal, Scope, DataAccess, Cache).
/// Derived booleans are provided for backward compatibility with existing behaviors.
/// </summary>
public sealed record RequestExecutionProfile(
    string RequestName,
    ApplicationRequestKind Kind,
    ApplicationPrincipalKind Principal,
    ApplicationScopeKind Scope,
    ApplicationDataAccessKind DataAccess,
    ApplicationCacheKind Cache,
    bool RequiresPermission,
    bool RequiresVerifiedEmail,
    bool RequiresSubscription,
    bool RequiresFeature,
    bool RequiresExpectedVersion,
    bool IsIdempotent,
    bool EmitsRealtime,
    bool EnqueuesPostCommit)
{
    // --- Derived booleans for backward compatibility ---

    public bool IsAnonymous => Principal == ApplicationPrincipalKind.Anonymous;
    public bool IsSystemInternal => Principal == ApplicationPrincipalKind.System;

    public bool IsGlobal => Scope == ApplicationScopeKind.Global;
    public bool IsAccountScoped => Scope == ApplicationScopeKind.Account;
    public bool IsWorkspaceScoped => Scope == ApplicationScopeKind.Workspace;
    public bool IsResourceScoped => Scope == ApplicationScopeKind.Resource;
    public bool IsTokenScoped => Scope == ApplicationScopeKind.Token;

    public bool IsTransactional => DataAccess == ApplicationDataAccessKind.Transactional;
    public bool IsRlsRead => DataAccess == ApplicationDataAccessKind.ReadOnly;

    public bool IsPublicCacheable => Cache == ApplicationCacheKind.Public;
    public bool IsAuthorizedCacheable => Cache == ApplicationCacheKind.Authorized;

    public bool IsRealtimeRequest => EmitsRealtime;

    // --- Composite derived properties ---

    public bool IsTenantScoped =>
        Scope is ApplicationScopeKind.Account
            or ApplicationScopeKind.Workspace
            or ApplicationScopeKind.Resource;

    public bool RequiresRls =>
        IsRlsRead
        || IsTenantScoped
        || RequiresPermission
        || RequiresSubscription
        || RequiresFeature;

    public bool NeedsDbScope =>
        DataAccess != ApplicationDataAccessKind.None || RequiresRls;

    public bool IsReadOnlyDbScope =>
        RequiresRls && DataAccess != ApplicationDataAccessKind.Transactional;
}
