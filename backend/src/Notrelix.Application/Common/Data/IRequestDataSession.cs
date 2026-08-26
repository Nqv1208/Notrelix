namespace Notrelix.Application.Common.Data;

using Notrelix.Domain.SharedKernel;

public enum RequestDataAccess
{
    None,
    ReadOnly,
    Transactional
}

public sealed record RequestDataSessionOptions(
    RequestDataAccess Access,
    bool ApplyTenantScope,
    bool ApplyResourceScope,
    ExpectedVersionConstraint? ExpectedVersion = null);

/// <summary>
/// Fail-closed optimistic-concurrency identity. The concrete request type and the
/// full <see cref="ResourceRef"/> travel with the constraint so Infrastructure can
/// bind it to exactly one tracked aggregate; a declared constraint that cannot be
/// bound is a server misconfiguration, never a silent skip.
/// </summary>
public sealed record ExpectedVersionConstraint(
    Type RequestType,
    ResourceRef Resource,
    long Value);

/// <summary>
/// Provider-independent data session port.
/// Application determines required data access; Infrastructure executes it.
/// </summary>
public interface IRequestDataSession
{
    Task<TResponse> ExecuteAsync<TResponse>(
        RequestDataSessionOptions options,
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken);
}
