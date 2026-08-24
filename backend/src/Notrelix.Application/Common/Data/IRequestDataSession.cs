namespace Notrelix.Application.Common.Data;

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

public sealed record ExpectedVersionConstraint(Guid ResourceId, long Value);

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
