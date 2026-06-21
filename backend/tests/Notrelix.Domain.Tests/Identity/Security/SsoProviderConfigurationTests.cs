using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Security;
using Xunit;

namespace Notrelix.Domain.Tests.Identity.Security;

public class SsoProviderConfigurationTests
{
    [Fact]
    public void FromMetadata_ShouldReturnEmpty_WhenJsonIsNull()
    {
        var config = SsoProviderConfiguration.FromMetadata(SsoProviderType.Saml, null);

        config.EntityId.Should().BeNull();
        config.SsoUrl.Should().BeNull();
        config.CertificateRef.Should().BeNull();
        config.Domain.Should().BeNull();
        config.RedirectUri.Should().BeNull();
    }

    [Fact]
    public void FromMetadata_ShouldParse_AllSamlFields()
    {
        var json = """
            {
                "entityId": "https://idp.example.com/metadata",
                "ssoUrl": "https://idp.example.com/sso",
                "certificateRef": "saml-cert-1",
                "domain": "example.com",
                "redirectUri": "https://app.notrelix.com/auth/saml/callback"
            }
            """;

        var config = SsoProviderConfiguration.FromMetadata(SsoProviderType.Saml, json);

        config.EntityId.Should().Be("https://idp.example.com/metadata");
        config.SsoUrl.Should().Be("https://idp.example.com/sso");
        config.CertificateRef.Should().Be("saml-cert-1");
        config.Domain.Should().Be("example.com");
        config.RedirectUri.Should().Be("https://app.notrelix.com/auth/saml/callback");
    }

    [Fact]
    public void FromMetadata_ShouldParse_OidcFields()
    {
        var json = """
            {
                "entityId": "my-oidc-app",
                "ssoUrl": "https://accounts.google.com/o/oauth2/v2/auth",
                "domain": "gmail.com"
            }
            """;

        var config = SsoProviderConfiguration.FromMetadata(SsoProviderType.Oidc, json);

        config.EntityId.Should().Be("my-oidc-app");
        config.SsoUrl.Should().Be("https://accounts.google.com/o/oauth2/v2/auth");
        config.Domain.Should().Be("gmail.com");
    }

    [Fact]
    public void FromMetadata_ShouldThrow_WhenSsoUrlIsNotAbsoluteUri()
    {
        var json = """{ "ssoUrl": "not-a-valid-url" }""";

        Action act = () => SsoProviderConfiguration.FromMetadata(SsoProviderType.Saml, json);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*SSO URL*");
    }

    [Fact]
    public void FromMetadata_ShouldThrow_WhenRedirectUriIsNotAbsoluteUri()
    {
        var json = """{ "redirectUri": "invalid" }""";

        Action act = () => SsoProviderConfiguration.FromMetadata(SsoProviderType.Oidc, json);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Redirect URI*");
    }

    [Fact]
    public void FromMetadata_ShouldAccept_MissingOptionalFields()
    {
        var json = """{}""";

        var config = SsoProviderConfiguration.FromMetadata(SsoProviderType.Google, json);

        config.EntityId.Should().BeNull();
        config.SsoUrl.Should().BeNull();
        config.CertificateRef.Should().BeNull();
        config.Domain.Should().BeNull();
        config.RedirectUri.Should().BeNull();
    }

    [Fact]
    public void Equals_ShouldCompareByAllProperties()
    {
        var json = """{ "entityId": "test", "ssoUrl": "https://example.com/sso" }""";
        var config1 = SsoProviderConfiguration.FromMetadata(SsoProviderType.Saml, json);
        var config2 = SsoProviderConfiguration.FromMetadata(SsoProviderType.Saml, json);

        config1.Equals(config2).Should().BeTrue();
        (config1 == config2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_ForDifferentProviderTypes()
    {
        var json = """{ "entityId": "test" }""";
        var config1 = SsoProviderConfiguration.FromMetadata(SsoProviderType.Saml, json);
        var config2 = SsoProviderConfiguration.FromMetadata(SsoProviderType.Oidc, json);

        config1.Equals(config2).Should().BeFalse();
    }
}
