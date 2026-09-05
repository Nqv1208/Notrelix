using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Public.Membership;
using Notrelix.Domain.Accounts.Accounts;

namespace Notrelix.Application.Features.Accounts.Members;

/// <summary>
/// Producer-owned implementation of the Accounts public membership admission
/// surface. Maps account lifecycle state into admission semantics inside
/// Application, where that business policy belongs.
/// </summary>
public sealed class AccountMembershipFactsProvider : IAccountMembershipFacts
{
    private readonly IAccountDbContext _context;

    public AccountMembershipFactsProvider(IAccountDbContext context)
    {
        _context = context;
    }

    public async Task<AccountMembershipAdmissionFact?> GetAdmissionAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var status = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId)
            .Select(a => (AccountStatus?)a.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (status is null)
            return null;

        var canAdmitMember = status is AccountStatus.Active or AccountStatus.Trialing;

        return new AccountMembershipAdmissionFact(canAdmitMember);
    }
}
