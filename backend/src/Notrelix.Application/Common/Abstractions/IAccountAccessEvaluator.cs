namespace Notrelix.Application.Common.Abstractions;

public interface IAccountAccessEvaluator
{
    Task<bool> HasAccountAccess(Guid accountId, CancellationToken cancellationToken = default);
    Task<bool> IsAccountAdmin(Guid accountId, CancellationToken cancellationToken = default);
}
