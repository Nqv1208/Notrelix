namespace Notrelix.Application.Common.Security;

public enum AccessDecisionKind
{
    Allowed,
    Unauthorized,
    Forbidden,
    NotFound,
    SecurityMisconfiguration
}

public sealed record AccessDecision(AccessDecisionKind Kind, string? Message = null)
{
    public static AccessDecision Allow() => new(AccessDecisionKind.Allowed);
    public static AccessDecision Deny(AccessDecisionKind kind, string message) => new(kind, message);
}
