using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Application.Features.Accounts.Abstractions;

public interface IAccountStatusReader
{
    Task<AccountStatus?> GetStatusAsync(
        Guid accountId,
        CancellationToken cancellationToken);
}
