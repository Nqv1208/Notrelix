using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Application.Features.Accounts.Sso.Commands.VerifySsoDomain;

/// <summary>
/// Looks up the domain's TXT records and verifies it on a match. A DNS miss, a timeout or an error leaves it Pending (never
/// a failure of the call); a domain another Account verified first is refused. Verifying a Verified domain is a no-op.
/// </summary>
public sealed record VerifySsoDomainCommand(Guid DomainId)
    : ICommand<Result<SsoDomainDto>>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;
}

public sealed class VerifySsoDomainCommandHandler : IRequestHandler<VerifySsoDomainCommand, Result<SsoDomainDto>>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISecretEncryptor _encryptor;
    private readonly IDomainTxtResolver _resolver;
    private readonly SsoDomainClaims _claims;

    public VerifySsoDomainCommandHandler(
        IAccountDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider clock,
        ISecretEncryptor encryptor,
        IDomainTxtResolver resolver,
        SsoDomainClaims claims)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _encryptor = encryptor;
        _resolver = resolver;
        _claims = claims;
    }

    public async Task<Result<SsoDomainDto>> Handle(VerifySsoDomainCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        var domain = await _context.AccountDomains.FirstOrDefaultAsync(d => d.Id == request.DomainId && d.AccountId == accountId, cancellationToken)
            ?? throw new NotFoundException(nameof(AccountDomain), request.DomainId);

        if (domain.VerificationStatus != DomainVerificationStatus.Pending)
            return Result<SsoDomainDto>.Success(SsoDomainDto.From(domain, _encryptor, claimed: false));

        if (await _claims.IsClaimedByAnotherAsync(domain.Domain, accountId, cancellationToken))
            throw new ConflictException("Another account has already verified this domain.", AccountSsoErrorCodes.DomainClaimed);

        if (!string.IsNullOrEmpty(domain.VerificationTokenProtected))
        {
            var token = _encryptor.Unprotect(domain.VerificationTokenProtected, AccountSsoSupport.DomainTokenPurpose);
            var records = await _resolver.ResolveTxtAsync(domain.Domain, cancellationToken);
            if (AccountSsoSupport.HasTxtValue(records, token))
            {
                domain.Verify(_clock.UtcNow, _currentUser.UserId);
                await _claims.SaveVerifiedAsync(cancellationToken);
            }
        }

        return Result<SsoDomainDto>.Success(SsoDomainDto.From(domain, _encryptor, claimed: false));
    }
}
