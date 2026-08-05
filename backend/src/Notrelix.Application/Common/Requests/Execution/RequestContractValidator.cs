namespace Notrelix.Application.Common.Requests.Execution;

/// <summary>
/// Validates critical marker combinations on a <see cref="RequestExecutionProfile"/>.
/// Returns a list of violation descriptions; empty means valid.
/// </summary>
public static class RequestContractValidator
{
    public static IReadOnlyList<string> Validate(RequestExecutionProfile profile)
    {
        var violations = new List<string>();

        // --- Principal rules ---

        if (profile.Principal == ApplicationPrincipalKind.Anonymous && profile.IsTenantScoped)
        {
            violations.Add("Anonymous request cannot be tenant/resource scoped.");
        }

        if (profile.Principal == ApplicationPrincipalKind.Anonymous && profile.RequiresPermission)
        {
            violations.Add("Anonymous request cannot require permission.");
        }

        if (profile.Principal == ApplicationPrincipalKind.System && profile.IsTenantScoped)
        {
            violations.Add("System-internal request should not be tenant-scoped. Use explicit operation identity.");
        }

        // --- Scope rules ---

        if (profile.IsGlobal && profile.RequiresPermission)
        {
            violations.Add("Global request cannot require tenant/resource permission.");
        }

        if (profile.IsTokenScoped && profile.IsTenantScoped)
        {
            violations.Add("Token-scoped request cannot also be account/workspace/resource scoped.");
        }

        if (profile.IsRlsRead && !profile.IsTenantScoped)
        {
            violations.Add("IRlsReadRequest must combine with a tenant-scoping interface.");
        }

        // --- Data access rules ---

        if (profile.Kind == ApplicationRequestKind.Query && profile.IsTransactional)
        {
            violations.Add("Query cannot be Transactional. Use None or ReadOnly.");
        }

        // --- Idempotency rules ---

        if (profile.IsIdempotent && profile.Kind != ApplicationRequestKind.Command)
        {
            violations.Add("Idempotent marker is only valid on Commands.");
        }

        if (profile.IsIdempotent && !profile.IsTransactional)
        {
            violations.Add("Idempotent command must be Transactional.");
        }

        if (profile.IsIdempotent && profile.Cache == ApplicationCacheKind.Public)
        {
            violations.Add("Idempotent command cannot use public cache.");
        }

        // --- Expected version rules ---

        if (profile.RequiresExpectedVersion && profile.Kind != ApplicationRequestKind.Command)
        {
            violations.Add("ExpectedVersion is only valid on Commands.");
        }

        if (profile.RequiresExpectedVersion && !profile.IsTransactional)
        {
            violations.Add("ExpectedVersion command must be Transactional.");
        }

        if (profile.RequiresExpectedVersion && profile.IsGlobal)
        {
            violations.Add("ExpectedVersion command must be Resource or Workspace scoped, not Global.");
        }

        // --- Realtime rules ---

        if (profile.EmitsRealtime && profile.Kind != ApplicationRequestKind.Command)
        {
            violations.Add("Realtime marker is only valid on Commands.");
        }

        if (profile.EmitsRealtime && !profile.IsTransactional)
        {
            violations.Add("Realtime command must be Transactional.");
        }

        // --- Cache rules ---

        if (profile.Cache == ApplicationCacheKind.Public && profile.IsTenantScoped)
        {
            violations.Add("Public cache cannot be used for tenant-scoped requests.");
        }

        if (profile.Cache == ApplicationCacheKind.Public
            && profile.Principal != ApplicationPrincipalKind.Anonymous
            && profile.Principal != ApplicationPrincipalKind.Authenticated)
        {
            violations.Add("Public cache requires Anonymous or Authenticated principal.");
        }

        if (profile.Cache == ApplicationCacheKind.Authorized && profile.EmitsRealtime)
        {
            violations.Add("Authorized-cacheable request cannot be realtime. Cache HIT skips handler, broadcasting stale data.");
        }

        if (profile.Cache == ApplicationCacheKind.Authorized
            && profile.Principal == ApplicationPrincipalKind.Anonymous)
        {
            violations.Add("Authorized cache requires Authenticated principal.");
        }

        return violations;
    }
}
