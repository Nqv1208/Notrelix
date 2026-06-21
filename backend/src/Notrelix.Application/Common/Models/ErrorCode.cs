namespace Notrelix.Application.Common.Models;

public static class ErrorCode
{
    public const string NotFound = "RESOURCE_NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string Validation = "VALIDATION_ERROR";
    public const string Conflict = "STATE_CONFLICT";
    public const string BusinessRule = "BUSINESS_RULE_VIOLATION";
    public const string Internal = "INTERNAL_SERVER_ERROR";
}
