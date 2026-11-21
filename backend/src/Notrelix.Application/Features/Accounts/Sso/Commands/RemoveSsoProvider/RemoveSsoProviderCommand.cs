using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Security;

namespace Notrelix.Application.Features.Accounts.Sso.Commands.RemoveSsoProvider;

/// <summary>Removes the Account's provider. Refused while single sign-on is required (the Account would lock its members out).</summary>
public sealed record RemoveSsoProviderCommand
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;
}

public sealed class RemoveSsoProviderCommandHandler : IRequestHandler<RemoveSsoProviderCommand, Result>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;

    public RemoveSsoProviderCommandHandler(IAccountDbContext context, ICurrentRequestContext currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveSsoProviderCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        var provider = await _context.AccountIdentityProviders.FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);
        if (provider is null) return Result.Success();

        if (await _context.AccountSettingsEntities.AnyAsync(
                s => s.AccountId == accountId && s.SettingKey == AccountSecuritySettingKeys.RequireSso
                    && s.SettingValue == AccountSecuritySettingKeys.True, cancellationToken))
            throw new ConflictException("Single sign-on is required: turn it off before removing the provider.", AccountSsoErrorCodes.RequiredInUse);

        _context.AccountIdentityProviders.Remove(provider);
        return Result.Success();
    }
}
