namespace Notrelix.Application.Common.Errors;

/// <summary>
/// Common cross-cutting Application error codes.
/// Format: lowercase dotted canonical (e.g., common.precondition-failed).
/// Values are stable once exposed by API v1.
/// </summary>
public static class CommonErrorCodes
{
    public const string ValidationFailed = "common.validation-failed";
    public const string NotFound = "common.not-found";
    public const string Conflict = "common.conflict";
    public const string PreconditionFailed = "common.precondition-failed";
    public const string IdempotencyInProgress = "common.idempotency-in-progress";
    public const string IdempotencyPayloadMismatch = "common.idempotency-payload-mismatch";
    public const string Forbidden = "common.forbidden";
    public const string Unauthorized = "common.unauthorized";
    public const string BusinessRuleViolation = "common.business-rule-violation";
}
