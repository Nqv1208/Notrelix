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
        "OperatorSignInByIp",
        "AuthRefreshByIp",
        "CsrfByIp",
        "AuthSensitiveByUser",
        "AuthByEmail",
        "GeneralAuthenticated",
        "GeneralAnonymous",
        "WebhookIntakeByIp",
        "CallMediaWebhookByIp",
        "ApiKeyAuthenticated",
    ];

    /// <summary>The Collaboration write limits (UC-COL-H05): one test user writes far more than 30 comments or 20 uploads a minute.</summary>
    public static readonly string[] CollaborationLimitNames = ["Comment", "React", "Upload"];

    public static IReadOnlyDictionary<string, string?> Generous { get; } = PolicyNames
        .Select(name => $"RateLimiting:Policies:{name}:PermitLimit")
        .Concat(CollaborationLimitNames.Select(name => $"RateLimiting:Collaboration:{name}:PermitLimit"))
        .ToDictionary(key => key, _ => (string?)GenerousPermitLimit);
}
