namespace Notrelix.Application.Common.Tenancy;

public interface IAccountAccessEvaluator
{
    Task<bool> HasAccountAccess(Guid accountId, CancellationToken cancellationToken = default);
    Task<bool> IsAccountAdmin(Guid accountId, CancellationToken cancellationToken = default);
}
