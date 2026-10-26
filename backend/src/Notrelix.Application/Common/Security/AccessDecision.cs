namespace Notrelix.Application.Common.Security;

public enum AccessDecisionKind
{
    Allowed,
    Unauthorized,
    Forbidden,
    NotFound,
    SecurityMisconfiguration
}

/// <summary>
/// Stable machine-readable reasons a Forbidden decision can carry. Clients branch on these,
/// never on the message text.
/// </summary>
public static class AccessDenialCodes
{
    public const string EmailNotConfirmed = "auth.email_not_confirmed";
}

public sealed record AccessDecision(AccessDecisionKind Kind, string? Message = null, string? Code = null)
{
    public static AccessDecision Allow() => new(AccessDecisionKind.Allowed);
    public static AccessDecision Deny(AccessDecisionKind kind, string message, string? code = null) => new(kind, message, code);
}
