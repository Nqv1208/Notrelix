using Notrelix.Domain.Accounts.Domains;
using Notrelix.Domain.Accounts.IdentityProviders;

namespace Notrelix.Application.Features.Accounts.Sso;

/// <summary>The Account's single sign-on setup as the Owner sees it. Never carries the client secret.</summary>
public sealed record AccountSsoDto(
    bool CanManage,
    string RedirectUri,
    bool RequireSso,
    SsoProviderDto? Provider,
    IReadOnlyList<SsoDomainDto> Domains);

public sealed record SsoProviderDto(
    string Name,
    string IssuerUrl,
    string ClientId,
    bool HasClientSecret,
    string Status,
    string? ValidationError,
    bool JitProvisioning)
{
    public static SsoProviderDto From(AccountIdentityProvider p) => new(
        p.Name, p.Issuer, p.ClientId ?? string.Empty, !string.IsNullOrEmpty(p.ClientSecretProtected),
        p.Status, p.ValidationError, p.JitProvisioningEnabled);
}

public sealed record SsoProviderSaveDto(string Status, string? ValidationError);

public sealed record SsoDomainDto(
    Guid Id,
    string Domain,
    string Status,
    string TxtHost,
    string? TxtValue,
    DateTimeOffset? VerifiedAt,
    bool Claimed)
{
    /// <summary>The DNS instructions are shown only while the claim is Pending (the token is public DNS data, not a secret).</summary>
    public static SsoDomainDto From(AccountDomain d, ISecretEncryptor encryptor, bool claimed) => new(
        d.Id,
        d.Domain,
        d.VerificationStatus.ToString(),
        d.Domain,
        d.VerificationStatus == DomainVerificationStatus.Pending && !string.IsNullOrEmpty(d.VerificationTokenProtected)
            ? AccountSsoSupport.TxtValue(encryptor.Unprotect(d.VerificationTokenProtected, AccountSsoSupport.DomainTokenPurpose))
            : null,
        d.VerifiedAt,
        claimed);
}
