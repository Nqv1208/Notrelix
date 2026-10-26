namespace Notrelix.Testing.Core;

/// <summary>
/// Configuration overrides that lift every API rate-limit policy far above what a test suite
/// can reach, so suites that call the auth endpoints in a loop from one loopback address are
/// never throttled. Tests that prove the shipped limits use the shipped values instead.
/// </summary>
public static class RateLimitTestSettings
{
    private const string GenerousPermitLimit = "1000000";

    public static readonly string[] PolicyNames =
    [
        "AuthCredentialByIp",
        "AuthRefreshByIp",
        "CsrfByIp",
        "AuthSensitiveByUser",
        "AuthByEmail",
        "GeneralAuthenticated",
        "GeneralAnonymous",
        "WebhookIntakeByIp",
        "ApiKeyAuthenticated",
    ];

    public static IReadOnlyDictionary<string, string?> Generous { get; } = PolicyNames.ToDictionary(
        name => $"RateLimiting:Policies:{name}:PermitLimit",
        name => (string?)GenerousPermitLimit);
}
