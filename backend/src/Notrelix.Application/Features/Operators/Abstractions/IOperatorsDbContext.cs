using Notrelix.Domain.Operators;

namespace Notrelix.Application.Features.Operators.Abstractions;

/// <summary>
/// The persistence of the platform operator principal and its audit trail. Reached only by the operator
/// database login; the tenant logins hold no privilege on these tables.
/// </summary>
public interface IOperatorsDbContext
{
    DbSet<OperatorAccount> OperatorAccounts { get; }
    DbSet<OperatorAuditEntry> OperatorAudit { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
