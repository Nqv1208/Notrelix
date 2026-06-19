using System.Text.Json;
using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Security;

public sealed class SsoProviderConfiguration : ValueObject
{
    public string? EntityId { get; }
    public string? SsoUrl { get; }
    public string? CertificateRef { get; }
    public SsoProviderType ProviderType { get; }
    public string? Domain { get; }
    public string? RedirectUri { get; }

    private SsoProviderConfiguration()
    {
    }

    private SsoProviderConfiguration(
        string? entityId,
        string? ssoUrl,
        string? certificateRef,
        SsoProviderType providerType,
        string? domain,
        string? redirectUri)
    {
        EntityId = entityId;
        SsoUrl = ssoUrl;
        CertificateRef = certificateRef;
        ProviderType = providerType;
        Domain = domain;
        RedirectUri = redirectUri;
    }

    public static SsoProviderConfiguration FromMetadata(SsoProviderType providerType, string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new SsoProviderConfiguration(
                null, null, null, providerType, null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            var root = doc.RootElement;

            var entityId = GetStringOrNull(root, "entityId");
            var ssoUrl = GetStringOrNull(root, "ssoUrl");
            var certificateRef = GetStringOrNull(root, "certificateRef");
            var domain = GetStringOrNull(root, "domain");
            var redirectUri = GetStringOrNull(root, "redirectUri");

            if (ssoUrl is not null && !Uri.TryCreate(ssoUrl, UriKind.Absolute, out _))
                throw new BusinessRuleException("SSO URL must be an absolute URI.");

            if (redirectUri is not null && !Uri.TryCreate(redirectUri, UriKind.Absolute, out _))
                throw new BusinessRuleException("Redirect URI must be an absolute URI.");

            return new SsoProviderConfiguration(
                entityId, ssoUrl, certificateRef, providerType, domain, redirectUri);
        }
        catch (JsonException)
        {
            return new SsoProviderConfiguration(
                null, null, null, providerType, null, null);
        }
    }

    private static string? GetStringOrNull(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();

        return null;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return EntityId;
        yield return SsoUrl;
        yield return CertificateRef;
        yield return ProviderType;
        yield return Domain;
        yield return RedirectUri;
    }
}
