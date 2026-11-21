using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Oidc;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Accounts.Security;
using Notrelix.Domain.Accounts.IdentityProviders;

namespace Notrelix.Application.Features.Accounts.Sso.Commands.SaveSsoProvider;

/// <summary>
/// Saves the Account's OIDC provider (one per Account; a second save replaces the configuration) and validates it by
/// reading its discovery document. The client secret is optional on update (the stored one is kept) and is never returned.
/// </summary>
public sealed record SaveSsoProviderCommand(string Name, string IssuerUrl, string ClientId, string? ClientSecret, bool JitProvisioning)
    : ICommand<Result<SsoProviderSaveDto>>, IWriteRequest, IAuthenticatedRequest, IAccountRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ManageAccountSecurity;
    public ResourceRef? Resource => null;

    // The secret never reaches a log line through the request's own ToString.
    public override string ToString() => $"SaveSsoProviderCommand {{ Name = {Name}, IssuerUrl = {IssuerUrl}, ClientId = {ClientId} }}";
}

public sealed class SaveSsoProviderCommandValidator : AbstractValidator<SaveSsoProviderCommand>
{
    public SaveSsoProviderCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(AccountSsoSupport.MaxNameLength);
        RuleFor(x => x.IssuerUrl).NotEmpty().MaximumLength(AccountSsoSupport.MaxIssuerLength);
        RuleFor(x => x.ClientId).NotEmpty().MaximumLength(AccountSsoSupport.MaxClientIdLength);
        RuleFor(x => x.ClientSecret).MaximumLength(AccountSsoSupport.MaxClientSecretLength);
    }
}

public sealed class SaveSsoProviderCommandHandler : IRequestHandler<SaveSsoProviderCommand, Result<SsoProviderSaveDto>>
{
    private readonly IAccountDbContext _context;
    private readonly ICurrentRequestContext _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISecretEncryptor _encryptor;
    private readonly IOidcDiscoveryClient _discovery;

    public SaveSsoProviderCommandHandler(
        IAccountDbContext context,
        ICurrentRequestContext currentUser,
        IDateTimeProvider clock,
        ISecretEncryptor encryptor,
        IOidcDiscoveryClient discovery)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
        _encryptor = encryptor;
        _discovery = discovery;
    }

    public async Task<Result<SsoProviderSaveDto>> Handle(SaveSsoProviderCommand request, CancellationToken cancellationToken)
    {
        var accountId = _currentUser.RequireAccountId();
        var userId = _currentUser.UserId;
        var now = _clock.UtcNow;

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > AccountSsoSupport.MaxNameLength)
            throw AccountSsoSupport.Unprocessable(AccountSsoErrorCodes.InvalidName, "The provider needs a display name.");
        var issuer = AccountSsoSupport.NormalizeIssuer(request.IssuerUrl)
            ?? throw AccountSsoSupport.Unprocessable(AccountSsoErrorCodes.InvalidIssuer, "The issuer must be an https address without credentials, query or fragment.");
        var clientId = request.ClientId?.Trim();
        if (string.IsNullOrEmpty(clientId) || clientId.Length > AccountSsoSupport.MaxClientIdLength)
            throw AccountSsoSupport.Unprocessable(AccountSsoErrorCodes.InvalidClient, "The provider needs a client id.");
        var secret = string.IsNullOrWhiteSpace(request.ClientSecret) ? null : request.ClientSecret.Trim();

        var provider = await _context.AccountIdentityProviders.FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);
        if (provider is null && secret is null)
            throw AccountSsoSupport.Unprocessable(AccountSsoErrorCodes.ClientSecretRequired, "The provider needs a client secret.");

        // Read the document before changing anything: a refused replacement must leave the working provider alone.
        var discovery = await _discovery.DiscoverAsync(issuer, cancellationToken);
        if (!discovery.Succeeded && provider is not null && await IsSsoRequiredAsync(accountId, cancellationToken))
            throw new ConflictException(
                "Single sign-on is required: the provider cannot be replaced by one that does not validate.",
                AccountSsoErrorCodes.RequiredInUse);

        var protectedSecret = secret is null ? null : _encryptor.Protect(secret, AccountSsoSupport.ClientSecretPurpose);
        if (provider is null)
        {
            provider = AccountIdentityProvider.CreateOidc(accountId, name, issuer, clientId, protectedSecret!, request.JitProvisioning, userId, now);
            _context.AccountIdentityProviders.Add(provider);
        }
        else
        {
            provider.UpdateOidc(name, issuer, clientId, protectedSecret, request.JitProvisioning, userId, now);
        }

        if (discovery.Document is { } document)
            provider.MarkValidated(document.AuthorizationEndpoint, userId, now);
        else
            provider.MarkValidationFailed(discovery.Failure ?? OidcDiscoveryFailure.DocumentInvalid, userId, now);

        return Result<SsoProviderSaveDto>.Success(new SsoProviderSaveDto(provider.Status, provider.ValidationError));
    }

    private Task<bool> IsSsoRequiredAsync(Guid accountId, CancellationToken cancellationToken) =>
        _context.AccountSettingsEntities.AsNoTracking().AnyAsync(
            s => s.AccountId == accountId && s.SettingKey == AccountSecuritySettingKeys.RequireSso
                && s.SettingValue == AccountSecuritySettingKeys.True, cancellationToken);
}
