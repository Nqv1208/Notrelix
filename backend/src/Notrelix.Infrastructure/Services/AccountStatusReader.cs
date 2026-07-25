using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Infrastructure.Services;

public sealed class AccountStatusReader : IAccountStatusReader
{
    private readonly IAccountDbContext _context;

    public AccountStatusReader(IAccountDbContext context)
    {
        _context = context;
    }

    public async Task<AccountStatus?> GetStatusAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => (AccountStatus?)a.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
