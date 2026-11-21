namespace Notrelix.Domain.Accounts.IdentityProviders;

public class AccountIdentityProvider : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string ProviderType { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Issuer { get; private set; } = null!;
    public string SsoUrl { get; private set; } = null!;
    public string? CertificateRef { get; private set; }
    public string Status { get; private set; } = StatusDraft;
    public bool JitProvisioningEnabled { get; private set; }

    public const string StatusDraft = "Draft";
    public const string StatusActive = "Active";
    public const string StatusDisabled = "Disabled";

    /// <summary>OIDC client id, as registered at the company's provider. Not a secret.</summary>
    public string? ClientId { get; private set; }

    /// <summary>The OIDC client secret, protected with the secret store. Never exposed by any read, response, log or event.</summary>
    public string? ClientSecretProtected { get; private set; }

    /// <summary>Why the last validation of the discovery document failed (a stable reason, never provider output). Null when none.</summary>
    public string? ValidationError { get; private set; }

    private AccountIdentityProvider() : base() { }

    public static AccountIdentityProvider Create(
        Guid accountId,
        string providerType,
        string name,
        string issuer,
        string ssoUrl,
        string? certificateRef = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(providerType);
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(issuer);
        Guard.NotNullOrWhiteSpace(ssoUrl);
        Guard.MaxLength(name, 120);
        Guard.MaxLength(issuer, 300);

        if (providerType != "Saml" && providerType != "Oidc")
            throw new BusinessRuleException(AccountRuleCodes.Accounts_IdentityProvider_InvalidProviderType, "Provider type must be 'Saml' or 'Oidc'.");

        return new AccountIdentityProvider
        {
            AccountId = accountId,
            ProviderType = providerType,
            Name = name.Trim(),
            Issuer = issuer.Trim(),
            SsoUrl = ssoUrl.Trim(),
            CertificateRef = certificateRef?.Trim(),
            JitProvisioningEnabled = false
        };
    }

    /// <summary>An OpenID Connect provider saved as Draft; it routes no sign-in until a validation makes it Active.</summary>
    public static AccountIdentityProvider CreateOidc(
        Guid accountId,
        string name,
        string issuer,
        string clientId,
        string protectedClientSecret,
        bool jitProvisioning,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        RequireClient(clientId, protectedClientSecret);
        var provider = Create(accountId, "Oidc", name, issuer, issuer);
        provider.ClientId = clientId.Trim();
        provider.ClientSecretProtected = protectedClientSecret;
        provider.JitProvisioningEnabled = jitProvisioning;
        provider.SetAuditOnCreate(createdBy, createdAt);
        return provider;
    }

    /// <summary>
    /// Replaces the OIDC configuration and returns the provider to Draft (it must be validated again). A null
    /// <paramref name="protectedClientSecret"/> keeps the stored secret.
    /// </summary>
    public void UpdateOidc(
        string name,
        string issuer,
        string clientId,
        string? protectedClientSecret,
        bool jitProvisioning,
        Guid updatedBy,
        DateTimeOffset updatedAt)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.NotNullOrWhiteSpace(issuer);
        Guard.MaxLength(name, 120);
        Guard.MaxLength(issuer, 300);
        RequireClient(clientId, protectedClientSecret ?? ClientSecretProtected);

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = name.Trim();
        Issuer = issuer.Trim();
        SsoUrl = Issuer;
        ClientId = clientId.Trim();
        if (protectedClientSecret is not null) ClientSecretProtected = protectedClientSecret;
        JitProvisioningEnabled = jitProvisioning;
        Status = StatusDraft;
        ValidationError = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    /// <summary>The discovery document was read and accepted: the provider is Active and routes sign-in.</summary>
    public void MarkValidated(string authorizationEndpoint, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotNullOrWhiteSpace(authorizationEndpoint);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        SsoUrl = authorizationEndpoint.Trim();
        Status = StatusActive;
        ValidationError = null;
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    /// <summary>The discovery document was refused: the provider stays Draft with a stable reason.</summary>
    public void MarkValidationFailed(string reason, Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotNullOrWhiteSpace(reason);
        Guard.MaxLength(reason, 120);
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = StatusDraft;
        ValidationError = reason.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
    }

    private static void RequireClient(string clientId, string? protectedSecret)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(protectedSecret))
            throw new BusinessRuleException(AccountRuleCodes.Accounts_IdentityProvider_ClientRequired, "An OIDC provider needs a client id and a client secret.");
        Guard.MaxLength(clientId, 255);
    }

    public void Enable()
    {
        if (Status == StatusActive) return;
        Status = StatusActive;
    }

    public void Disable()
    {
        if (Status == StatusDisabled) return;
        Status = StatusDisabled;
    }

    public void EnableJitProvisioning()
    {
        JitProvisioningEnabled = true;
    }

    public void DisableJitProvisioning()
    {
        JitProvisioningEnabled = false;
    }

    public void UpdateCertificate(string certificateRef)
    {
        Guard.NotNullOrWhiteSpace(certificateRef);
        CertificateRef = certificateRef.Trim();
    }
}
