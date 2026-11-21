using FluentAssertions;
using Notrelix.Domain.Accounts.IdentityProviders;

namespace Notrelix.Domain.Tests.Accounts;

public class AccountIdentityProviderTests
{
    private readonly Guid _accountId = Guid.NewGuid();

    [Fact]
    public void Create_WithSamlProvider_ShouldSucceed()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta SSO", "https://okta.example.com", "https://okta.example.com/sso", "cert-ref");

        idp.AccountId.Should().Be(_accountId);
        idp.ProviderType.Should().Be("Saml");
        idp.Name.Should().Be("Okta SSO");
        idp.Issuer.Should().Be("https://okta.example.com");
        idp.SsoUrl.Should().Be("https://okta.example.com/sso");
        idp.CertificateRef.Should().Be("cert-ref");
        idp.Status.Should().Be("Draft");
        idp.JitProvisioningEnabled.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOidcProvider_ShouldSucceed()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Oidc", "Azure AD", "https://login.microsoftonline.com/tenant", "https://login.microsoftonline.com/tenant/oauth2");

        idp.ProviderType.Should().Be("Oidc");
        idp.CertificateRef.Should().BeNull();
    }

    [Fact]
    public void Create_WithInvalidProviderType_ShouldThrow()
    {
        var act = () => AccountIdentityProvider.Create(
            _accountId, "Ldap", "LDAP", "https://ldap.example.com", "https://ldap.example.com/sso");

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Saml*Oidc*");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => AccountIdentityProvider.Create(
            _accountId, "Saml", "  ", "https://issuer.example.com", "https://sso.example.com");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => AccountIdentityProvider.Create(
            Guid.Empty, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Enable_ShouldSetStatusToActive()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");

        idp.Enable();

        idp.Status.Should().Be("Active");
    }

    [Fact]
    public void Enable_WhenAlreadyActive_ShouldBeIdempotent()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");
        idp.Enable();

        idp.Enable();

        idp.Status.Should().Be("Active");
    }

    [Fact]
    public void Disable_ShouldSetStatusToDisabled()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");
        idp.Enable();

        idp.Disable();

        idp.Status.Should().Be("Disabled");
    }

    [Fact]
    public void EnableJitProvisioning_ShouldSetToTrue()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Oidc", "Azure AD", "https://issuer.example.com", "https://sso.example.com");

        idp.EnableJitProvisioning();

        idp.JitProvisioningEnabled.Should().BeTrue();
    }

    [Fact]
    public void DisableJitProvisioning_ShouldSetToFalse()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Oidc", "Azure AD", "https://issuer.example.com", "https://sso.example.com");
        idp.EnableJitProvisioning();

        idp.DisableJitProvisioning();

        idp.JitProvisioningEnabled.Should().BeFalse();
    }

    [Fact]
    public void UpdateCertificate_ShouldUpdateRef()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com", "old-cert");

        idp.UpdateCertificate("new-cert");

        idp.CertificateRef.Should().Be("new-cert");
    }

    [Fact]
    public void UpdateCertificate_WithEmptyValue_ShouldThrow()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");

        var act = () => idp.UpdateCertificate("  ");

        act.Should().Throw<BusinessRuleException>();
    }

    private AccountIdentityProvider Oidc(bool jit = true) => AccountIdentityProvider.CreateOidc(
        _accountId, "Acme SSO", "https://login.acme.example", "client-1", "cipher-1", jit, Guid.NewGuid(), DateTimeOffset.UtcNow);

    [Fact]
    public void CreateOidc_StartsAsDraft_WithClientIdProtectedSecretAndJitFlag()
    {
        var idp = Oidc(jit: false);

        idp.ProviderType.Should().Be("Oidc");
        idp.Status.Should().Be(AccountIdentityProvider.StatusDraft);
        idp.ClientId.Should().Be("client-1");
        idp.ClientSecretProtected.Should().Be("cipher-1");
        idp.JitProvisioningEnabled.Should().BeFalse();
        idp.ValidationError.Should().BeNull();
    }

    [Fact]
    public void CreateOidc_WithoutClientIdOrSecret_Throws()
    {
        var blankId = () => AccountIdentityProvider.CreateOidc(_accountId, "n", "https://i.example", " ", "c", true, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var blankSecret = () => AccountIdentityProvider.CreateOidc(_accountId, "n", "https://i.example", "id", " ", true, Guid.NewGuid(), DateTimeOffset.UtcNow);

        blankId.Should().Throw<BusinessRuleException>();
        blankSecret.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void MarkValidated_MakesItActive_AndClearsTheReason()
    {
        var idp = Oidc();
        idp.MarkValidationFailed("unreachable", Guid.NewGuid(), DateTimeOffset.UtcNow);

        idp.MarkValidated("https://login.acme.example/authorize", Guid.NewGuid(), DateTimeOffset.UtcNow);

        idp.Status.Should().Be(AccountIdentityProvider.StatusActive);
        idp.ValidationError.Should().BeNull();
        idp.SsoUrl.Should().Be("https://login.acme.example/authorize");
    }

    [Fact]
    public void MarkValidationFailed_LeavesItDraft_WithTheStatedReason()
    {
        var idp = Oidc();

        idp.MarkValidationFailed("issuer-mismatch", Guid.NewGuid(), DateTimeOffset.UtcNow);

        idp.Status.Should().Be(AccountIdentityProvider.StatusDraft);
        idp.ValidationError.Should().Be("issuer-mismatch");
    }

    [Fact]
    public void UpdateOidc_ReturnsToDraft_KeepsTheStoredSecretWhenNoneIsGiven_AndReplacesItWhenGiven()
    {
        var idp = Oidc();
        idp.MarkValidated("https://login.acme.example/authorize", Guid.NewGuid(), DateTimeOffset.UtcNow);

        idp.UpdateOidc("Acme 2", "https://login.acme.example", "client-2", null, false, Guid.NewGuid(), DateTimeOffset.UtcNow);

        idp.Status.Should().Be(AccountIdentityProvider.StatusDraft);
        idp.ClientId.Should().Be("client-2");
        idp.ClientSecretProtected.Should().Be("cipher-1", "an update without a secret keeps the stored one");
        idp.JitProvisioningEnabled.Should().BeFalse();

        idp.UpdateOidc("Acme 2", "https://login.acme.example", "client-2", "cipher-2", false, Guid.NewGuid(), DateTimeOffset.UtcNow);
        idp.ClientSecretProtected.Should().Be("cipher-2");
    }
}
