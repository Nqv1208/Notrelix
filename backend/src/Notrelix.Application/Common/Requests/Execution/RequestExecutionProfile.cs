namespace Notrelix.Application.Common.Requests.Execution;

public sealed record RequestExecutionProfile(
    string RequestName,
    bool IsAnonymous,
    bool IsSystemInternal,
    bool IsGlobal,
    bool IsAccountScoped,
    bool IsWorkspaceScoped,
    bool IsResourceScoped,
    bool IsTokenScoped,
    bool IsTransactional,
    bool IsRlsRead,
    bool RequiresPermission,
    bool RequiresSubscription,
    bool RequiresFeature,
    bool IsPublicCacheable,
    bool IsAuthorizedCacheable,
    bool IsRealtimeRequest)
{
    public bool IsTenantScoped =>
        IsAccountScoped || IsWorkspaceScoped || IsResourceScoped;

    public bool RequiresRls =>
        IsRlsRead
        || IsTenantScoped
        || RequiresPermission
        || RequiresSubscription
        || RequiresFeature;

    public bool NeedsDbScope =>
        IsTransactional || RequiresRls;

    public bool IsReadOnlyDbScope =>
        RequiresRls && !IsTransactional;
}
