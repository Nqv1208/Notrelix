using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Security;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Application.Features.Accounts.Sso.Queries.GetAccountSso;

/// <summary>The Account's SSO setup. Owner only (ManageAccountSecurity is Owner-locked): an Admin or a Member is refused.</summary>
public sealed record GetAccountSsoQuery
    : IQuery<Result<AccountSsoDto>>, IAuthenticatedRequest, IAccountRequest, IReadRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;
}

public sealed class GetAccountSsoQueryHandler : IRequestHandler<GetAccountSsoQuery, Result<AccountSsoDto>>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IAccountSecurityManageAuthorizer _manage;
    private readonly ISecretEncryptor _encryptor;
    private readonly SsoDomainClaims _claims;

    public GetAccountSsoQueryHandler(
        IAccountDbContext context,
        ICurrentRequestContext currentUser,
        IAccountSecurityManageAuthorizer manage,
        ISecretEncryptor encryptor,
        SsoDomainClaims claims)
    {
        _context = context;
        _currentUser = currentUser;
        _manage = manage;
        _encryptor = encryptor;
        _claims = claims;
    }

    public async Task<Result<AccountSsoDto>> Handle(GetAccountSsoQuery request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();

        var provider = await _context.AccountIdentityProviders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);
        var domains = await _context.AccountDomains.AsNoTracking()
            .Where(d => d.AccountId == accountId)
            .OrderBy(d => d.Domain)
            .ToListAsync(cancellationToken);
        var required = await _context.AccountSettingsEntities.AsNoTracking().AnyAsync(
            s => s.AccountId == accountId && s.SettingKey == AccountSecuritySettingKeys.RequireSso
                && s.SettingValue == AccountSecuritySettingKeys.True, cancellationToken);

        var rows = new List<SsoDomainDto>(domains.Count);
        foreach (var domain in domains)
        {
            var claimed = domain.VerificationStatus == DomainVerificationStatus.Pending
                && await _claims.IsClaimedByAnotherAsync(domain.Domain, accountId, cancellationToken);
            rows.Add(SsoDomainDto.From(domain, _encryptor, claimed));
        }

        return Result<AccountSsoDto>.Success(new AccountSsoDto(
            await _manage.CanManageAsync(cancellationToken),
            RedirectUri: string.Empty, // the endpoint fills in the callback address of this deployment
            required,
            provider is null ? null : SsoProviderDto.From(provider),
            rows));
    }
}
