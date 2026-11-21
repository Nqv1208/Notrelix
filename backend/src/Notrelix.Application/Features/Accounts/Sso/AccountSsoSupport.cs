using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Notrelix.Application.Features.Accounts.Sso;

/// <summary>Stable public codes of the single sign-on administration (the problem <c>errorCode</c>).</summary>
public static class AccountSsoErrorCodes
{
    public const string DomainInvalid = "accounts.domain.invalid";
    public const string DomainPublicProvider = "accounts.domain.public-provider";
    public const string DomainClaimed = "accounts.domain.claimed";
    public const string InvalidIssuer = "accounts.sso.invalid-issuer";
    public const string InvalidName = "accounts.sso.invalid-name";
    public const string InvalidClient = "accounts.sso.invalid-client";
    public const string ClientSecretRequired = "accounts.sso.client-secret-required";
    public const string NotReady = "accounts.sso.not-ready";
    public const string RequiredInUse = "accounts.sso.required-in-use";
}

/// <summary>Protection purposes, the DNS record shape and the domain rules of single sign-on.</summary>
public static class AccountSsoSupport
{
    /// <summary>Purpose of the secret store for the OIDC client secret (a ciphertext in the table, never the plaintext).</summary>
    public const string ClientSecretPurpose = "Notrelix.Accounts.Sso.ClientSecret.v1";

    /// <summary>Purpose for the DNS TXT token (it ends up in public DNS; protected so it is not a plain column).</summary>
    public const string DomainTokenPurpose = "Notrelix.Accounts.Sso.DomainToken.v1";

    public const string TxtPrefix = "notrelix-verify=";

    public const int MaxIssuerLength = 300;
    public const int MaxNameLength = 120;
    public const int MaxClientIdLength = 255;
    public const int MaxClientSecretLength = 1024;

    private static readonly Regex Label = new("^(?!-)[a-z0-9-]{1,63}(?<!-)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Mail providers anyone can register on: never an organization's domain.</summary>
    private static readonly HashSet<string> PublicMailProviders = new(StringComparer.Ordinal)
    {
        "gmail.com", "googlemail.com", "outlook.com", "hotmail.com", "live.com", "msn.com", "yahoo.com", "ymail.com",
        "icloud.com", "me.com", "mac.com", "proton.me", "protonmail.com", "pm.me", "aol.com", "gmx.com", "gmx.net",
        "yandex.com", "yandex.ru", "mail.com", "mail.ru", "qq.com", "163.com", "126.com", "zoho.com", "fastmail.com",
        "hey.com", "tutanota.com", "tuta.io", "web.de", "naver.com", "rediffmail.com",
    };

    /// <summary>The lowercase ASCII host name, or null when the input is not a plain domain name (no scheme, path, port, address, single label or non-ASCII).</summary>
    public static string? NormalizeDomain(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var domain = input.Trim().TrimEnd('.').ToLowerInvariant();
        if (domain.Length is 0 or > 253) return null;
        var labels = domain.Split('.');
        if (labels.Length < 2 || labels.Any(l => !Label.IsMatch(l))) return null;
        if (labels[^1].All(char.IsDigit)) return null; // an IPv4 address is not a domain
        return domain;
    }

    public static bool IsPublicMailProvider(string domain) => PublicMailProviders.Contains(domain);

    /// <summary>The domain after the last '@' of an address, lowercase; null when the address has none.</summary>
    public static string? DomainOfEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var at = email.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1) return null;
        return email[(at + 1)..].Trim().TrimEnd('.').ToLowerInvariant();
    }

    public static string NewDomainToken() => Base64Url(RandomNumberGenerator.GetBytes(32));

    public static string TxtValue(string token) => TxtPrefix + token;

    public static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();

    /// <summary>True when any record of the domain is exactly the expected TXT value (surrounding quotes and spaces ignored).</summary>
    public static bool HasTxtValue(IEnumerable<string> records, string token)
    {
        var expected = TxtValue(token);
        return records.Any(r => string.Equals(r.Trim().Trim('"').Trim(), expected, StringComparison.Ordinal));
    }

    /// <summary>The absolute https issuer without credentials, query or fragment, or null.</summary>
    public static string? NormalizeIssuer(string? issuer)
    {
        if (string.IsNullOrWhiteSpace(issuer) || issuer.Length > MaxIssuerLength) return null;
        if (!Uri.TryCreate(issuer.Trim(), UriKind.Absolute, out var uri)) return null;
        if (uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host)) return null;
        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return null;
        return issuer.Trim();
    }

    public static UnprocessableException Unprocessable(string code, string message) => new(message, code);

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
