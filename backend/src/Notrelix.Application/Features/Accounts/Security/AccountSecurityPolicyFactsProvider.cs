using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Public.Security;
using Notrelix.Domain.Accounts.Members;

namespace Notrelix.Application.Features.Accounts.Security;

public sealed class AccountSecurityPolicyFactsProvider : IAccountSecurityPolicyFacts
{
    private readonly IAccountDbContext _context;

    public AccountSecurityPolicyFactsProvider(IAccountDbContext context)
    {
        _context = context;
    }

    public Task<bool> IsMfaRequiredAsync(Guid accountId, CancellationToken cancellationToken) =>
        _context.AccountSettingsEntities
            .AsNoTracking()
            .AnyAsync(
                s => s.AccountId == accountId
                    && s.SettingKey == AccountSecuritySettingKeys.RequireMfa
                    && s.SettingValue == AccountSecuritySettingKeys.True,
                cancellationToken);

    public Task<bool> IsMfaRequiredForPersonAsync(Guid userId, CancellationToken cancellationToken) =>
        _context.AccountSettingsEntities
            .AsNoTracking()
            .Where(s => s.SettingKey == AccountSecuritySettingKeys.RequireMfa
                && s.SettingValue == AccountSecuritySettingKeys.True)
            .AnyAsync(
                s => _context.AccountMembers.Any(m =>
                    m.AccountId == s.AccountId && m.UserId == userId && m.Status == AccountMemberStatus.Active),
                cancellationToken);
}
