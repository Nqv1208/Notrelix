using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Abstractions.Records;

namespace Notrelix.Application.Features.Accounts.Security.Commands.SetAccountMfaPolicy;

public class SetAccountMfaPolicyCommandHandler : IRequestHandler<SetAccountMfaPolicyCommand, Result>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SetAccountMfaPolicyCommandHandler(
        IAccountDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(SetAccountMfaPolicyCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        var userId = _currentUser.UserId;

        var value = request.Required ? AccountSecuritySettingKeys.True : AccountSecuritySettingKeys.False;
        var now = _dateTimeProvider.UtcNow;
        var setting = await _context.AccountSettingsEntities.FirstOrDefaultAsync(
            s => s.AccountId == accountId && s.SettingKey == AccountSecuritySettingKeys.RequireMfa,
            cancellationToken);

        if (setting is null)
        {
            _context.AccountSettingsEntities.Add(new AccountSettingRecord
            {
                Id = Guid.CreateVersion7(),
                AccountId = accountId,
                SettingKey = AccountSecuritySettingKeys.RequireMfa,
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
