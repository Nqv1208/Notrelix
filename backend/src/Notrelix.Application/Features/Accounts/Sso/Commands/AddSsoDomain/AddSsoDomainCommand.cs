using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Domain.Accounts.Domains;

namespace Notrelix.Application.Features.Accounts.Sso.Commands.AddSsoDomain;

/// <summary>
/// Claims a domain for the Account: a Pending row with the DNS TXT instructions. Adding a domain the Account already has
/// returns the existing row; a domain another Account holds Verified is refused.
/// </summary>
public sealed record AddSsoDomainCommand(string Domain)
    : ICommand<Result<SsoDomainDto>>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;
}

public sealed class AddSsoDomainCommandValidator : AbstractValidator<AddSsoDomainCommand>
{
    public AddSsoDomainCommandValidator()
    {
        RuleFor(x => x.Domain).NotEmpty().MaximumLength(253);
    }
}

public sealed class AddSsoDomainCommandHandler : IRequestHandler<AddSsoDomainCommand, Result<SsoDomainDto>>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISecretEncryptor _encryptor;
    private readonly SsoDomainClaims _claims;

    public AddSsoDomainCommandHandler(
        IAccountDbContext context, ICurrentRequestContext currentUser, IDateTimeProvider clock, ISecretEncryptor encryptor, SsoDomainClaims claims)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _encryptor = encryptor;
        _claims = claims;
    }

    public async Task<Result<SsoDomainDto>> Handle(AddSsoDomainCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();

        var domain = AccountSsoSupport.NormalizeDomain(request.Domain)
            ?? throw AccountSsoSupport.Unprocessable(AccountSsoErrorCodes.DomainInvalid, "Enter a domain name such as example.com.");
        if (AccountSsoSupport.IsPublicMailProvider(domain))
            throw AccountSsoSupport.Unprocessable(AccountSsoErrorCodes.DomainPublicProvider, "A public mail provider cannot be an organization domain.");

        var existing = _context.AccountDomains.Local.FirstOrDefault(d => d.AccountId == accountId && d.Domain == domain)
            ?? await _context.AccountDomains.FirstOrDefaultAsync(d => d.AccountId == accountId && d.Domain == domain, cancellationToken);
        if (existing is not null)
            return Result<SsoDomainDto>.Success(SsoDomainDto.From(existing, _encryptor, claimed: false));

        if (await _claims.IsClaimedByAnotherAsync(domain, accountId, cancellationToken))
            throw new ConflictException("Another account has already verified this domain.", AccountSsoErrorCodes.DomainClaimed);

        var token = AccountSsoSupport.NewDomainToken();
        var created = AccountDomain.Create(
            accountId, domain, _currentUser.UserId, _clock.UtcNow,
            AccountSsoSupport.HashToken(token), _encryptor.Protect(token, AccountSsoSupport.DomainTokenPurpose));
        _context.AccountDomains.Add(created);
        return Result<SsoDomainDto>.Success(SsoDomainDto.From(created, _encryptor, claimed: false));
    }
}
