using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Members;
using Notrelix.Application.Features.Accounts.Public.PersonalAccountProvisioning;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Accounts.Provisioning;

/// <summary>
/// Creates the personal Account and its owner AccountMember on the Accounts
/// DbContext. Does not save or commit — the request transaction commits both
/// Identity and Accounts atomically (BOUND-TX-004).
/// </summary>
public sealed class AccountProvisioningService : IAccountProvisioningActions
{
    private readonly IAccountDbContext _context;
    private readonly IAccountGrantProjectionService _grantProjection;

    public AccountProvisioningService(IAccountDbContext context, IAccountGrantProjectionService grantProjection)
    {
        _context = context;
        _grantProjection = grantProjection;
    }

    public async Task<PersonalAccountProvisioningResult> ProvisionPersonalAccountAsync(
        Guid userId,
        string displayName,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var accountName = PersonalAccountName.For(displayName);
        var account = Account.Create(
            accountName,
            UniqueSlugFor(accountName),
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

        await _grantProjection.SyncAccountMemberGrantAsync(account.Id, userId, AccountRole.Owner, occurredAt, cancellationToken);

        return new PersonalAccountProvisioningResult(account.Id);
    }

    /// <summary>
    /// The slug of a personal Account: the name's slug plus a random tail, so two people with the same display
    /// name never meet. Another Account's slug cannot be read here (the registration scope sees only the new
    /// Account), so uniqueness is not checked, it is built in: the tail is 60 random bits. The unique index
    /// <c>idx_account_slug</c> is the guarantee behind it; a slug asked for by hand takes its conflict from there.
    /// </summary>
    internal static string UniqueSlugFor(string accountName)
    {
        var tail = Guid.NewGuid().ToString("N")[..16];
        var baseSlug = Slug.GenerateFromName(accountName).Value;
        if (baseSlug.Length > MaxBaseLength)
            baseSlug = baseSlug[..MaxBaseLength].TrimEnd('-');
        return $"{baseSlug}-{tail}";
    }

    // 128 characters in the column: the base, a hyphen and the 16-character tail fit with room to spare.
    private const int MaxBaseLength = 100;
}
