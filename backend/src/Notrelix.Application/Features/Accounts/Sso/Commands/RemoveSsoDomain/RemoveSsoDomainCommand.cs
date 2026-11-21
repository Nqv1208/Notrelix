using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Security;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Application.Features.Accounts.Sso.Commands.RemoveSsoDomain;

/// <summary>Removes a domain claim. The last Verified domain cannot go while single sign-on is required.</summary>
public sealed record RemoveSsoDomainCommand(Guid DomainId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;
}

public sealed class RemoveSsoDomainCommandHandler : IRequestHandler<RemoveSsoDomainCommand, Result>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;

    public RemoveSsoDomainCommandHandler(IAccountDbContext context, ICurrentRequestContext currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveSsoDomainCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        var domain = await _context.AccountDomains.FirstOrDefaultAsync(d => d.Id == request.DomainId && d.AccountId == accountId, cancellationToken);
        if (domain is null) return Result.Success();

        if (domain.VerificationStatus == DomainVerificationStatus.Verified
            && !await _context.AccountDomains.AnyAsync(
                d => d.AccountId == accountId && d.Id != domain.Id && d.VerificationStatus == DomainVerificationStatus.Verified, cancellationToken)
            && await _context.AccountSettingsEntities.AnyAsync(
                s => s.AccountId == accountId && s.SettingKey == AccountSecuritySettingKeys.RequireSso
                    && s.SettingValue == AccountSecuritySettingKeys.True, cancellationToken))
            throw new ConflictException("Single sign-on is required: turn it off before removing the last verified domain.", AccountSsoErrorCodes.RequiredInUse);

        _context.AccountDomains.Remove(domain);
        return Result.Success();
    }
}
