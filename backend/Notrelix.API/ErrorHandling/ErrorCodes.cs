namespace Notrelix.API.ErrorHandling;

public static class ErrorCodes
{
    public const string ValidationFailed = "validation.failed";
    public const string BusinessRuleViolation = "business_rule.violation";
    public const string Unauthorized = "auth.unauthorized";
    public const string Forbidden = "auth.forbidden";
    public const string ResourceNotFound = "resource.not_found";
    public const string Conflict = "concurrency.conflict";
    public const string InternalServerError = "internal_server_error";
}
