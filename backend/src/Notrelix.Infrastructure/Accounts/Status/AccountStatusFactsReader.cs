using Notrelix.Application.Features.Accounts.Public.Status;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Accounts.Status;

public sealed class AccountStatusFactsReader : IAccountStatusFacts
{
    private readonly ApplicationDbContext _context;

    public AccountStatusFactsReader(ApplicationDbContext context) => _context = context;

    public async Task<AccountStatusFact> GetAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var status = await _context.Accounts.AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => (AccountStatus?)a.Status)
            .FirstOrDefaultAsync(cancellationToken);

        return status switch
        {
            AccountStatus.Active or AccountStatus.Trialing => AccountStatusFact.Active,
            AccountStatus.Suspended => AccountStatusFact.Suspended,
            // Closed, and an Account that cannot be read: the stricter reading.
            _ => AccountStatusFact.Closed,
        };
    }
}
