namespace Notrelix.Domain.Accounts.IdentityProviders;

public class AccountIdentityProvider : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string ProviderType { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Issuer { get; private set; } = null!;
    public string SsoUrl { get; private set; } = null!;
    public string? CertificateRef { get; private set; }
    public string Status { get; private set; } = "Draft";
    public bool JitProvisioningEnabled { get; private set; }

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
            throw new BusinessRuleException("Provider type must be 'Saml' or 'Oidc'.");

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

    public void Enable()
    {
        if (Status == "Active") return;
        Status = "Active";
    }

    public void Disable()
    {
        if (Status == "Disabled") return;
        Status = "Disabled";
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
