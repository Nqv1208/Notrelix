using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions.Records;
using Notrelix.Application.Features.Accounts.Security;
using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;

namespace Notrelix.Application.Features.Accounts.Sso.Commands.SetSsoRequired;

/// <summary>
/// Turns the single sign-on requirement on or off (Owner only). It can be turned on only while the Account has an Active
/// provider and at least one Verified domain.
/// </summary>
public sealed record SetSsoRequiredCommand(bool Required)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;
}

public sealed class SetSsoRequiredCommandHandler : IRequestHandler<SetSsoRequiredCommand, Result>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _clock;

    public SetSsoRequiredCommandHandler(IAccountDbContext context, ICurrentRequestContext currentUser, IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result> Handle(SetSsoRequiredCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        if (request.Required
            && (!await _context.AccountIdentityProviders.AnyAsync(
                    p => p.AccountId == accountId && p.Status == AccountIdentityProvider.StatusActive, cancellationToken)
                || !await _context.AccountDomains.AnyAsync(
                    d => d.AccountId == accountId && d.VerificationStatus == DomainVerificationStatus.Verified, cancellationToken)))
            throw new ConflictException(
                "Single sign-on needs an active provider and at least one verified domain.", AccountSsoErrorCodes.NotReady);

        var value = request.Required ? AccountSecuritySettingKeys.True : AccountSecuritySettingKeys.False;
        var setting = await _context.AccountSettingsEntities.FirstOrDefaultAsync(
            s => s.AccountId == accountId && s.SettingKey == AccountSecuritySettingKeys.RequireSso, cancellationToken);

        if (setting is null)
        {
            _context.AccountSettingsEntities.Add(new AccountSettingRecord
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                SettingKey = AccountSecuritySettingKeys.RequireSso,
                SettingValue = value,
                CreatedAt = now,
                CreatedBy = userId
            });
        }
        else if (setting.SettingValue != value)
        {
            setting.SettingValue = value;
            setting.UpdatedAt = now;
            setting.UpdatedBy = userId;
        }

        return Result.Success();
    }
}
