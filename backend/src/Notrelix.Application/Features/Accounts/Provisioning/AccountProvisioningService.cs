using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Accounts.Provisioning;

/// <summary>
/// Creates the personal Account and its owner AccountMember on the Accounts
/// DbContext. Does not save or commit — the request transaction commits both
/// Identity and Accounts atomically.
/// </summary>
public sealed class AccountProvisioningService : IAccountProvisioningService
{
    private readonly IAccountDbContext _context;

    public AccountProvisioningService(IAccountDbContext context)
    {
        _context = context;
    }

    public Task<PersonalAccountProvisioningResult> ProvisionPersonalAccountAsync(
        Guid userId,
        string displayName,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var accountName = $"{displayName}'s Account";
        var accountSlug = Slug.GenerateFromName(accountName);
        var account = Account.Create(
            accountName,
            accountSlug.Value,
            AccountType.Personal,
            userId,
            occurredAt);

        _context.Accounts.Add(account);

        _context.AccountMembers.Add(AccountMember.Create(
            account.Id,
            userId,
            AccountRole.Owner,
            userId,
            occurredAt));

        return Task.FromResult(new PersonalAccountProvisioningResult(account.Id));
    }
}
