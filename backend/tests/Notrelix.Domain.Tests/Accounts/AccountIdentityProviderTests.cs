using FluentAssertions;
using Notrelix.Domain.Accounts.IdentityProviders;
using Notrelix.Domain.Tests.Freeze;

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

    [CoversMutation(typeof(AccountIdentityProvider), "Enable()", MutationScenario.Valid)]
    [Fact]
    public void Enable_ShouldSetStatusToActive()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");

        idp.Enable();

        idp.Status.Should().Be("Active");
    }

    [CoversMutation(typeof(AccountIdentityProvider), "Enable()", MutationScenario.NoOp)]
    [Fact]
    public void Enable_WhenAlreadyActive_ShouldBeIdempotent()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");
        idp.Enable();

        idp.Enable();

        idp.Status.Should().Be("Active");
    }

    [CoversMutation(typeof(AccountIdentityProvider), "Disable()", MutationScenario.Valid)]
    [Fact]
    public void Disable_ShouldSetStatusToDisabled()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");
        idp.Enable();

        idp.Disable();

        idp.Status.Should().Be("Disabled");
    }

    [CoversMutation(typeof(AccountIdentityProvider), "EnableJitProvisioning()", MutationScenario.Valid)]
    [Fact]
    public void EnableJitProvisioning_ShouldSetToTrue()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Oidc", "Azure AD", "https://issuer.example.com", "https://sso.example.com");

        idp.EnableJitProvisioning();

        idp.JitProvisioningEnabled.Should().BeTrue();
    }

    [CoversMutation(typeof(AccountIdentityProvider), "DisableJitProvisioning()", MutationScenario.Valid)]
    [Fact]
    public void DisableJitProvisioning_ShouldSetToFalse()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Oidc", "Azure AD", "https://issuer.example.com", "https://sso.example.com");
        idp.EnableJitProvisioning();

        idp.DisableJitProvisioning();

        idp.JitProvisioningEnabled.Should().BeFalse();
    }

    [CoversMutation(typeof(AccountIdentityProvider), "UpdateCertificate(System.String)", MutationScenario.Valid)]
    [Fact]
    public void UpdateCertificate_ShouldUpdateRef()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com", "old-cert");

        idp.UpdateCertificate("new-cert");

        idp.CertificateRef.Should().Be("new-cert");
    }

    [CoversMutation(typeof(AccountIdentityProvider), "UpdateCertificate(System.String)", MutationScenario.Invalid)]
    [Fact]
    public void UpdateCertificate_WithEmptyValue_ShouldThrow()
    {
        var idp = AccountIdentityProvider.Create(
            _accountId, "Saml", "Okta", "https://issuer.example.com", "https://sso.example.com");

        var act = () => idp.UpdateCertificate("  ");

        act.Should().Throw<BusinessRuleException>();
    }
}
